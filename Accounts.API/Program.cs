using Accounts.API.Abstractions;
using Accounts.API.Auth;
using Accounts.API.Crypto;
using Accounts.API.Filters;
using Accounts.API.Health;
using Accounts.API.Infrastructure.Mongo;
using Accounts.API.Mappers;
using Accounts.API.Middleware;
using Accounts.API.Services;
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

    //// Example: Log startup details
    //Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    //Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);

    // Add services to the container.

    // Configure Serilog
    builder.Logging.ClearProviders();
    builder.Host.UseSerilog((ctx, lc) => lc
           .ReadFrom.Configuration(ctx.Configuration));
           // .Enrich.FromLogContext());
           // .ReadFrom.Services()    // DI-based enrichers
           // .WriteTo.Console());  // causes double logging if also configured in appsettings with args

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
    //// DUENDE AUTH CONFIG
    //JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear(); // Note: As configured, Roles are not populated by HttpContext.User.Claims without this
    //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //    .AddJwtBearer(options =>
    //    {
    //        options.Authority = "https://localhost:5001";   // IDP
    //        options.Audience = "accountsapi";            // this api, middleware checks value is in token  
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

    // JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
    // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); 

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority should match the issurer (`iss`) of the JWT returned by the identity provider.
        options.Authority = builder.Configuration["AZURE_CREDENTIALS_AUTHORITY"];
        // Received Token: "aud": "0a5c9d6e-24c4-4fec-a018-e6f682d31921", "iss": "https://login.microsoftonline.com/2fd80906-88f0-4874-8d94-1d87e82053f7/v2.0",
        // NOTE this 'idp' = live.com and BLAZOR CONSUMER 'idp' = https://sts.windows.net/9188040d-6c67-4c5b-b112-36a304b66dad/     
        // Audience is this API's Application ID URI
        options.Audience = builder.Configuration["AZURE_CREDENTIALS_AUDIENCE"];         
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // make sure claims are mapped consistently
            NameClaimType = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Name,
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

    //builder.Services.Configure<MongoSettings>(builder.Configuration.GetRequiredSection(nameof(MongoSettings)));
    //builder.Services.AddSingleton<IMongoSettings>(sp => sp.GetRequiredService<IOptions<MongoSettings>>().Value);

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
    app.UseMiddleware<DevelopmentOnlyMiddleware>();
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

    app.MapGet("/", () => "Accounts API is up and running. Last Build: 1 Jun 2026 @ 22:24 CST").AllowAnonymous();

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
