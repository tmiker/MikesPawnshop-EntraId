using HealthChecks.UI.Client;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Products.Write.API;
using Products.Write.API.ExceptionHandling;
using Products.Write.API.ExceptionHandling.ExceptionHandlers;
using Products.Write.API.Middleware;
using Products.Write.Application.DTOs;
using Products.Write.Application.Extensions;
using Products.Write.Domain.Enumerations;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

// Configure static logger early for capturing startup issues
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting application ...");

    var builder = WebApplication.CreateBuilder(args);

    // Example: Log startup details
    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);

    // Configure Serilog
    builder.Logging.ClearProviders();
    builder.Host.UseSerilog((ctx, lc) => lc
           .ReadFrom.Configuration(ctx.Configuration));

    // Add HealthChecks with SQL Server check
    builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Environment.IsDevelopment() ? builder.Configuration["LOCAL_SQL_CONNECTIONSTRING"]! : builder.Configuration["AZURE_SQL_WRITE_CONNECTIONSTRING"]!,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "db", "sql", "sqlserver" }
    );

    builder.Services.AddProblemDetails(); // Registers the ProblemDetails service - configured in ExceptionHandlers using ExceptionHandlerExtensions 

    builder.Services.AddCors(setup =>
    {
        setup.AddPolicy("AllowAnyPolicy", policy =>
        {
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.WithExposedHeaders("X-Pagination");
        });
    });

    // MS ENTRA ID AUTH CONFIG
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority should match the issurer (`iss`) of the JWT returned by the identity provider.
        options.Authority = builder.Configuration["AZURE_CREDENTIALS_AUTHORITY"];   
        // Audience is this API's Application ID URI
        options.Audience = builder.Configuration["AZURE_CREDENTIALS_AUDIENCE"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // make sure claims are mapped consistently
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "roles",                                                // roles plural to match Entra Id implementation of roles
            ValidIssuer = builder.Configuration["AZURE_CREDENTIALS_VALID_ISSUER"]
            // Validate ...
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("IsAdmin", policy => policy.RequireClaim("roles", "Admin"));                     
        options.AddPolicy("IsManager", policy => policy.RequireClaim("roles", "Manager"));                     
        options.AddPolicy("IsAdminOrManager", policy => policy.RequireClaim("roles", "Admin", "Manager"));     
        options.AddPolicy("MarlowAndWendy", policy => policy.RequireClaim(ClaimTypes.Name, "Wendy Davenport", "Marlow Bean"));
        options.AddPolicy("DomesticDogs", policy => policy.RequireClaim("Genus", "Canis").RequireClaim("Species", "Familiaris"));
    });

    // CONFIGURE MEDIATR AND PIPELINE BEHAVIORS
    builder.Services.AddMediatR(cfg =>
    {
        cfg.LicenseKey = builder.Configuration.GetValue<string>("MEDIATR_LICENSE_KEY");
        cfg.RegisterServicesFromAssembly(typeof(Products.Write.Application.DIRegistrations).Assembly);
        // Register pipeline behaviors in order
        // 1. Logging - use Serilog
        // 2. Validation - FluentValidation - change ValidationExceptionHandler to use FluentValidation.ValidationException
        // 3. Handle exceptions - use ExceptionHandlers
        // 4. Monitor performance - Serilog Request Logging
        // 5. Manage transactions
    });

    // Register services from Composition Root
    string? environmentName = builder.Environment.EnvironmentName;
    builder.Services.ComposeApplication(environmentName);

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Register exception handlers in order of specificity (most specific first)
    builder.Services.AddExceptionHandler<ProductEventStoreExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Backup handler

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseMiddleware<SerilogMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseExceptionHandler(); // Enables the middleware to use the registered IExceptionHandler above

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle($"Pawn Shop Products Write Side API");
            options.WithTheme(ScalarTheme.Mars);
            options.EnableDarkMode();
        });
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAnyPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/", () => "Products.Write API is up and running. Last Build: 20 Jul 2026 @ 16:50 CST");

    // YARP healthcheck endpoint
    app.MapHealthChecks("/api/productsManagement/healthYarp", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();    //.RequireAuthorization("IsAdminOrManager");     

    // Client healthcheck endpoint
    app.MapHealthChecks("/api/productsManagement/healthClient", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };

            HealthCheckResultDTO dto = new HealthCheckResultDTO()
            {
                Status = report.Status.ToString(),
                TotalDuration = report.TotalDuration.TotalMilliseconds + "ms"
            };
            if (report.Entries is not null && report.Entries.Any())
            {
                dto.Entries = new Dictionary<string, HealthCheckResultEntriesDTO>();
                foreach (var entry in report.Entries)
                {
                    dto.Entries.Add(entry.Key, new HealthCheckResultEntriesDTO() { Status = entry.Value.Status.ToString(), Description = entry.Value.Description, Duration = entry.Value.Duration.ToString() });
                }
            }

            if (report.Status == HealthStatus.Healthy) app.Logger.LogHealthCheckStatus(report.Status.ToString());
            //// DefaultHealthCheckService automatically logs Unhealthy result already, so no need to log error
            //else
            //{
            //    string jsonResult = JsonSerializer.Serialize(dto);
            //    app.Logger.LogError("Health Check Result: {jsonResult}", jsonResult);
            //}

            //// dev purposes only
            // string jsonResult = JsonSerializer.Serialize(dto);
            // app.Logger.LogInformation("Health Check Result: {jsonResult}", jsonResult);

            await context.Response.WriteAsync(JsonSerializer.Serialize(dto, options));
        }

    }).RequireAuthorization("IsAdminOrManager");

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}