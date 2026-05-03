using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Orders.API.Abstractions;
using Orders.API.Auth;
using Orders.API.Crypto;
using Orders.API.Health;
using Orders.API.Infrastructure.Mongo;
using Orders.API.Middleware;
using Orders.API.Services;
using Orders.API.Utility;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Net.Http.Headers;
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

    // Configure Serilog using appsettings 
    builder.Logging.ClearProviders();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration));

    builder.Services.AddHealthChecks()
        .AddCheck<MongoDbHealthCheck>(name: "MongoLocalConnectionHealthCheck");

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

    // Configure Auth
    //// DUENDE AUTH CONFIG
    //JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear(); // Note: As configured, Roles are not populated by HttpContext.User.Claims without this
    //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //    .AddJwtBearer(options =>
    //    {
    //        options.Authority = "https://localhost:5001";   // IDP
    //        options.Audience = "ordersapi";            // this api, middleware checks value is in token  
    //        options.TokenValidationParameters = new TokenValidationParameters()
    //        {
    //            NameClaimType = "given_name",       // should have the same mapping as in client app
    //            RoleClaimType = "role",             // should have the same mapping as in our client mvc app
    //            ValidTypes = new[] { "at+jwt" }     // says the only valid token type is 'at + jwt'
    //        };

    //        //// Optional: Keep claim names as in token
    //        //options.MapInboundClaims = false;
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

    builder.Services.AddScoped<ITokenDecoder, TokenDecoder>();

    builder.Services.Configure<MongoSettings>(builder.Configuration.GetRequiredSection(nameof(MongoSettings)));
    builder.Services.AddSingleton<IMongoSettings>(sp => sp.GetRequiredService<IOptions<MongoSettings>>().Value);

    builder.Services.AddScoped<IOrderService, OrderService>();

    builder.Services.AddHttpClient(name: StaticData.InternalAccounts_HttpClient_Name, configureClient: config =>
    {
        // uses API Key auth for intermal api to api communication
        config.BaseAddress = new Uri(StaticData.InternalAccounts_HttpClient_BaseUrl);
        config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 1.0));
    });
    builder.Services.AddSingleton<IInternalAccountsHttpService, InternalAccountsHttpService>();

    // *** Crypto Services *** //
    builder.Services.AddScoped<IAesSymmetricEncryptionManager, AesSymmetricEncryptionManager>();
    builder.Services.AddScoped<IRsaAsymmetricEncryptionManager, RsaAsymmetricEncryptionManager>();
    builder.Services.AddScoped<IRsaAsymmetricKeyContainerManager, RsaAsymmetricKeyContainerManager>();
    builder.Services.AddScoped<IKeyContainerService, KeyContainerService>();
    // *** Crypto Services *** //

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SerilogMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Orders API");
            options.WithTheme(ScalarTheme.Default);
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

    app.MapGet("/", () => "Hello, his is the Orders API.");

    //// YARP healthcheck endpoint - uncomment if configure YARP HealthChecks for Orders - use this URL in YARP
    //app.MapHealthChecks("/api/orders/healthYarp", new HealthCheckOptions
    //{
    //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    //}).RequireAuthorization();

    // Client healthcheck endpoint
    app.MapHealthChecks("/api/orders/healthClient", new HealthCheckOptions
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
