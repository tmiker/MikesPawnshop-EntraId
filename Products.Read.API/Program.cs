using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Products.Read.API;
using Products.Read.API.DTOs;
using Products.Read.API.Extensions;
using Products.Read.API.Health;
using Products.Read.API.Infrastructure.Data;
using Products.Read.API.MessageConsumers;
using Products.Read.API.Middleware;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Security.Authentication;
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

    //// Example: Log startup details
    //Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    //Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);

    // Configure Serilog
    builder.Logging.ClearProviders();
    builder.Host.UseSerilog((ctx, lc) => lc
           .ReadFrom.Configuration(ctx.Configuration));

    //builder.Services.AddHealthChecks()
    //    .AddSqlServer(builder.Configuration.GetConnectionString("LocalDevelopmentConnectionString")!);

    // Add HealthChecks with SQL Server check
    builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Environment.IsDevelopment() ? builder.Configuration["LOCAL_SQL_CONNECTIONSTRING"]! : builder.Configuration["AZURE_SQL_READ_CONNECTIONSTRING"]!,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "db", "sql", "sqlserver" }
    );

    //builder.Services.AddHealthChecks()
    //    // Add a health check for a SQL Server database
    //    .AddCheck(
    //        name: "SqlServer",
    //        instance: new SqlServerHealthCheck(builder.Configuration.GetConnectionString("LocalDevelopmentConnectionString")!),
    //        failureStatus: HealthStatus.Unhealthy,
    //        tags: new string[] { "sql", "sqlserver" });

    builder.Services.AddCors(setup =>
    {
        setup.AddPolicy("AllowGetPolicy", policy =>
        {
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
            policy.WithMethods("GET");
            policy.WithExposedHeaders("X-Pagination");
        });
    });

    builder.Services.AddProblemDetails();

    // Configure Auth
    //// DUENDE AUTH CONFIG
    //JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear(); // Note: As configured, Roles are not populated by HttpContext.User.Claims without this
    //builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    //    .AddJwtBearer(options =>
    //    {
    //        options.Authority = "https://localhost:5001";
    //        options.Audience = "productsreadapi";
    //        options.TokenValidationParameters = new TokenValidationParameters()
    //        {
    //            NameClaimType = "given_name",       // should have the same mapping as in client app
    //            RoleClaimType = "role",             // should have the same mapping as in our client mvc app
    //            ValidTypes = new[] { "at+jwt" }     // says the only valid token type is 'at + jwt' 
    //                                                //ValidateIssuer = true,
    //                                                //ValidateAudience = true,
    //                                                //ValidateLifetime = true
    //        };

    //    });
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

    /// RESPONSE CACHING
    builder.Services.AddResponseCaching();
    /// OUTPUT CACHING
    //builder.Services.AddOutputCache(options =>
    //{
    //    //options.AddBasePolicy(builder =>
    //    //{
    //    //    builder.Expire(TimeSpan.FromSeconds(30));
    //    //    builder.Tag("products");
    //    //});
    //    options.AddPolicy("SixtySecondsCache", builder =>
    //    {
    //        builder.Expire(TimeSpan.FromSeconds(60));
    //        builder.Tag("products");
    //    });
    //    options.AddPolicy("NoCache", builder =>
    //    {
    //        builder.NoCache();
    //    });
    //});

    // Add database context and cache
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDbContext<ProductsReadDbContext>(options =>
            options.UseSqlServer(builder.Configuration["LOCAL_SQL_CONNECTIONSTRING"]));
    }
    else
    {
        builder.Services.AddDbContext<ProductsReadDbContext>(options =>
            options.UseSqlServer(builder.Configuration["AZURE_SQL_READ_CONNECTIONSTRING"]));
    }

    //// THE BELOW WORKS IN DEV AND GIT WORKFLOW (WITH ENV VARIABLES ADDED) 
    string amqpUrl = builder.Configuration["CLOUD_AMQP_SETTINGS_URL"] ?? throw new ArgumentNullException("Invalid Cloud AMQP URL configuration.");
    string amqpUsername = builder.Configuration["CLOUD_AMQP_SETTINGS_USERVHOST"] ?? throw new ArgumentNullException("Invalid Cloud AMQP User VHost configuration.");
    string amqpPassword = builder.Configuration["CLOUD_AMQP_SETTINGS_PASSWORD"] ?? throw new ArgumentNullException("Invalid Cloud AMQP Password configuration.");
    //// TRY UNIQUE APPROACH FOR EACH ENVIRONMENT
    //string amqpUrl = env.IsDevelopment() ?
    //    configuration.GetSection("CloudAMQPSettings:Url").Value! :          // local dev
    //    configuration["CloudAMQPSettings:Url"]! ??                          // GitHub actions
    //    configuration.GetValue<string>("CloudAMQPSettings__Url") ??         // Azure App Service 
    //    throw new ArgumentNullException("Invalid Cloud AMQP URL configuration.");
    //string amqpUsername = env.IsDevelopment() ? 
    //    configuration.GetSection("CloudAMQPSetting:UserVhost").Value! :     // local dev
    //    configuration["CloudAMQPSetting:UserVhost"]! ??                     // GitHub actions
    //    configuration.GetValue<string>("CloudAMQPSetting__UserVhost") ??    // Azure App Service
    //    throw new ArgumentNullException("Invalid Cloud AMQP User VHost configuration.");
    //string amqpPassword = env.IsDevelopment() ? 
    //    configuration.GetSection("CloudAMQPSettings:Password").Value! :     // local dev
    //    configuration["CloudAMQPSettings:Password"]! ??                     // GitHub actions
    //    configuration.GetValue<string>("CloudAMQPSettings__Password")! ??   // Azure App Service
    //    throw new ArgumentNullException("Invalid Cloud AMQP Password configuration.");

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<ProductAddedConsumer>();
        x.AddConsumer<StatusUpdateConsumer>();
        x.AddConsumer<DocumentAddedConsumer>();
        x.AddConsumer<ImageAddedConsumer>();
        x.AddConsumer<DocumentDeletedConsumer>();
        x.AddConsumer<ImageDeletedConsumer>();
        x.AddConsumer<DataPurgedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(amqpUrl), h =>
            {
                h.Username(amqpUsername);
                h.Password(amqpPassword);

                h.UseSsl(s =>
                {
                    s.Protocol = SslProtocols.Tls12;
                });
            });
            cfg.ReceiveEndpoint("ProductsReadApi1Queue", e =>
            {
                e.ConfigureConsumeTopology = false; // explicit is safer for versioning

                e.ConfigureConsumer<ProductAddedConsumer>(context);
                e.ConfigureConsumer<StatusUpdateConsumer>(context);
                e.ConfigureConsumer<DocumentAddedConsumer>(context);
                e.ConfigureConsumer<ImageAddedConsumer>(context);
                e.ConfigureConsumer<DocumentDeletedConsumer>(context);
                e.ConfigureConsumer<ImageDeletedConsumer>(context);
                e.ConfigureConsumer<DataPurgedConsumer>(context);

                // Robustness: retry with jitter + immediate faults to _error queue if exhausted
                e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                e.PrefetchCount = 4;    // 16;    // 16;
                e.ConcurrentMessageLimit = 1;   // 8;  // 8;
            });
        });
    });

    //// Register services from Composition Root
    // IWebHostEnvironment env = builder.Environment;
    builder.Services.ComposeApplication();

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    app.UseMiddleware<SerilogMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle($"Pawn Shop Products Read Side API");
            options.WithTheme(ScalarTheme.DeepSpace);
            options.EnableDarkMode();
        });
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowGetPolicy");

    app.UseResponseCaching();
    // app.UseOutputCache();   // must be called after UseCors and after UseRouting if called

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/", () => "Hello, this is the Products.Read API.");

    // YARP healthcheck endpoint
    app.MapHealthChecks("/api/products/healthYarp", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();    //.RequireAuthorization("IsAdminOrManager");

    // Client healthcheck endpoint
    app.MapHealthChecks("/api/products/healthClient", new HealthCheckOptions
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

    }).AllowAnonymous();

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