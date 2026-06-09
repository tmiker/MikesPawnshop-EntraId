using Carts.API.Abstractions;
using Carts.API.Auth;
using Carts.API.Health;
using Carts.API.Infrastructure.Mongo;
using Carts.API.Middleware;
using Carts.API.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Security.Claims;

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

    builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>(name: "MongoHealthCheck");

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

    //// Replaced with direct retrieval from configuration
    //builder.Services.Configure<MongoSettings>(builder.Configuration.GetRequiredSection(nameof(MongoSettings)));
    //builder.Services.AddSingleton<IMongoSettings>(sp => sp.GetRequiredService<IOptions<MongoSettings>>().Value);

    // Best Practice:
    // Always explicitly set NameClaimType and RoleClaimType in TokenValidationParameters so your code is not dependent on defaults that may change.

    // Configure Auth
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
        options.AddPolicy("IsAdmin", policy => policy.RequireClaim("roles", "Admin"));                          // (ClaimTypes.Role, "Admin")); does not work
        options.AddPolicy("IsManager", policy => policy.RequireClaim("roles", "Manager"));                      // (ClaimTypes.Role, "Manager")); does not work
        options.AddPolicy("IsAdminOrManager", policy => policy.RequireClaim("roles", "Admin", "Manager"));      // (ClaimTypes.Role, "Admin", "Manager"));does not work
        options.AddPolicy("MarlowAndWendy", policy => policy.RequireClaim(ClaimTypes.Name, "Wendy Davenport", "Marlow Bean"));
        options.AddPolicy("DomesticDogs", policy => policy.RequireClaim("Genus", "Canis").RequireClaim("Species", "Familiaris"));
    });

    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<ITokenDecoder, TokenDecoder>();

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SerilogMiddleware>();
    // app.UseMiddleware<CustomLoggingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Carts API");
            options.WithTheme(ScalarTheme.Solarized);
            options.EnableDarkMode();
        });
    }

    // Enable Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors("AllowAnyPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/", () => "Carts API is up and running. Last Build: 8 Jun 2026 @ 23:35 CST");

    //// YARP healthcheck endpoint - uncomment if configure YARP HealthChecks for Carts - use this URL in YARP
    //app.MapHealthChecks("/api/carts/healthYarp", new HealthCheckOptions
    //{
    //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    //}).RequireAuthorization();

    // Client healthcheck endpoint
    app.MapHealthChecks("/api/carts/healthClient", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).RequireAuthorization();

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