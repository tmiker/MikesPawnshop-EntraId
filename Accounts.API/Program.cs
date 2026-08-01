using Accounts.API.Abstractions;
using Accounts.API.Auth;
using Accounts.API.Crypto;
using Accounts.API.Filters;
using Accounts.API.Health;
using Accounts.API.Mappers;
using Accounts.API.Middleware;
using Accounts.API.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

    //// Log startup details
    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);

    // Configure Serilog
    builder.Logging.ClearProviders();
    builder.Host.UseSerilog((ctx, lc) => lc
           .ReadFrom.Configuration(ctx.Configuration));
           // .Enrich.FromLogContext());
           // .ReadFrom.Services()          // DI-based enrichers
           // .WriteTo.Console());          // causes double logging if also configured in appsettings with args

    builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>(name: "MongoHealthCheck"); // was MongoLocalConnectionHealthCheck

    builder.Services.AddCors(setup =>
    {
        setup.AddPolicy("AllowAnyPolicy", policy =>
        {
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.WithExposedHeaders("X-Pagination", "X-OrdersToAccounts-API-Key");
        });
    });

    // Configure Auth 
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
            NameClaimType = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Name,
            RoleClaimType = "roles",                                                                // roles plural to match Entra Id implementation of roles
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

    builder.Services.AddScoped<ITokenDecoder, TokenDecoder>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<IAccountDataMapper, AccountDataMapper>();
    builder.Services.AddScoped<IInternalAccountService, InternalAccountService>();

    // *** API KEY AUTH *** //
    builder.Services.AddTransient<IOrdersApiKeyValidator, OrdersApiKeyValidator>();
    builder.Services.AddScoped<OrdersApiKeyAuthFilter>();
    builder.Services.AddHttpContextAccessor();
    // *** API KEY AUTH *** //

    // *** Crypto Services *** //
    builder.Services.AddScoped<IEncryptionHelper, EncryptionHelper>();
    // the below require Cryptographic Services API (CAPI) and are deprecated
    builder.Services.AddScoped<IAesSymmetricEncryptionManager, AesSymmetricEncryptionManager>();
    builder.Services.AddScoped<IRsaAsymmetricEncryptionManager, RsaAsymmetricEncryptionManager>();
    builder.Services.AddScoped<IRsaAsymmetricKeyContainerManager, RsaAsymmetricKeyContainerManager>();
    builder.Services.AddScoped<IKeyContainerService, KeyContainerService>();
    // *** Crypto Services *** //

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // app.UseMiddleware<DevelopmentOnlyMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SerilogMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Accounts API");
            options.WithTheme(ScalarTheme.Alternate);
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

    app.MapGet("/", () => "Accounts API is up and running. Last Build: 18 Jul 2026 @ 18:20 CST").AllowAnonymous();

    //// YARP healthcheck endpoint - uncomment if configure YARP HealthChecks for Accounts - use this URL in YARP
    //app.MapHealthChecks("/api/accounts/healthYarp", new HealthCheckOptions
    //{
    //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    //}).RequireAuthorization();

    // Client healthcheck endpoint
    app.MapHealthChecks("/api/accounts/healthClient", new HealthCheckOptions
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
