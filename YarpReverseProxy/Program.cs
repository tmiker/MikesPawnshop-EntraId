using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
IdentityModelEventSource.ShowPII = true;  // Enable detailed error messages for token validation issues (useful for debugging, but should be disabled in production)
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;  // Prevent automatic mapping of standard JWT claims to Microsoft-specific claim types (e.g. "sub" to "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd", subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetRequiredSection("YarpProxySettings"))
    .AddTransforms(transforms =>
    {
        transforms.AddRequestTransform(async context =>
        {
            string? token = await context.HttpContext.GetTokenAsync("access_token");
            
            if (!string.IsNullOrEmpty(token))
            {
                // context.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"\n************** YARP Reverse Proxy - Transform - Access Token: *************** \n{token}\n");
            }
            else Console.WriteLine($"\n************** YARP Reverse Proxy - Transform - ACCESS TOKEN IS NULL ***************\n");
        });
    }); 

builder.Services.AddAuthorization();

// IDX10214: Audience validation failed. See https://aka.ms/identitymodel/app-context-switches

//builder.Services.AddAuthentication("Bearer")
//.AddJwtBearer("Bearer", options =>
//{
//    options.Authority = builder.Configuration["AZURE_CREDENTIALS_AUTHORITY"];       //  "https://login.microsoftonline.com/{tenantId}/v2.0";
//    // options.Audience = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];        // "{clientId}";
//    options.Audience = "https://localhost:7245";
//    options.TokenValidationParameters.ValidateAudience = true;
//});

//builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApp(msIdentityOptions =>
//    {
//        msIdentityOptions.Authority = builder.Configuration["AZURE_CREDENTIALS_AUTHORITY"];     // "https://login.microsoftonline.com/2fd80906-88f0-4874-8d94-1d87e82053f7/v2.0";

//        // msIdentityOptions.CallbackPath = "/signin-oidc";                        // for local development
//        // msIdentityOptions.CallbackPath = "/.auth/login/aad/callback";        // for azure hosted

//        // msIdentityOptions.SignedOutCallbackPath = "/signout-callback-oidc";             // for local development
//        // msIdentityOptions.SignedOutCallbackPath = "/.auth/logout/aad/callback";      // for azure hosted

//        msIdentityOptions.ClientId = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];                // for azure deploy

//        // msIdentityOptions.ClientSecret = builder.Configuration["AZURE_CREDENTIALS_CLIENT_SECRET"];        // for azure deploy

//        msIdentityOptions.Domain = builder.Configuration["AZURE_CREDENTIALS_DOMAIN"];                    // for azure deploy

//        msIdentityOptions.Instance = "https://login.microsoftonline.com/";

//        // msIdentityOptions.ResponseType = "code";

//        msIdentityOptions.TenantId = builder.Configuration["AZURE_CREDENTIALS_TENANT_ID"];                // for azure deploy

//        //msIdentityOptions.GetClaimsFromUserInfoEndpoint = true;
//        //msIdentityOptions.MapInboundClaims = false;
//        //msIdentityOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
//        //msIdentityOptions.TokenValidationParameters.RoleClaimType = "roles";
//        //msIdentityOptions.SaveTokens = true;
//        //msIdentityOptions.AccessDeniedPath = new PathString("/AccessDenied");

//    })
//     .EnableTokenAcquisitionToCallDownstreamApi()
//     .AddDistributedTokenCaches();

//builder.Services.AddDistributedMemoryCache();

//builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(
//    options =>
//    {
//        //options.DisableL1Cache = false;                           // Disable L1 Cache default: false
//        //options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;     // L1 Cache Size Limit default: 500 MB
//        // options.Encrypt = true;                                     // Encrypt tokens at rest default: false
//        //options.SlidingExpiration = TimeSpan.FromHours(1);        // Sliding Expiration default: 1 hour
//    });
/// MS ENTRA ID AUTH CONFIG END



/// TRY THIS!!!
//builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);   // looks for config section named "AzureAd" by default, but can be overridden with second parameter to specify different section name (e.g. "AzureAdB2C" for Azure AD B2C)

// builder.Services.AddAuthorization();


//// ADD RATE LIMITING POLICIES 
/// Rate Limiting Policies can be global or named - named are specified by endpoint or page

//// 1. Global Rate Limiting Policy that permits 10 requests per minute by user (identity) or globally:
//builder.Services.AddRateLimiter(options =>
//{
//    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//        RateLimitPartition.GetFixedWindowLimiter(
//            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
//            factory: partition => new FixedWindowRateLimiterOptions
//            {
//                AutoReplenishment = true,
//                PermitLimit = 10,
//                QueueLimit = 0,
//                Window = TimeSpan.FromMinutes(1)
//            }));
//});

// 2. Named Rate Limiting Policy to be added to specific endpoints or globally (see yarp_notes.txt):
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("BasicRateLimitingPolicy", opt =>
    {
        opt.PermitLimit = 4;
        opt.Window = TimeSpan.FromSeconds(12);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

//// ADD REVERSE PROXY
//builder.Services.AddReverseProxy()
//    .LoadFromConfig(builder.Configuration.GetRequiredSection("YarpProxySettings"));
//// ADD REVERSE PROXY WITH TRANSORM FOR PASSING TOKENS
//builder.Services.AddReverseProxy()
//    .LoadFromConfig(builder.Configuration.GetRequiredSection("YarpProxySettings"))
//    .AddTransforms(transforms =>
//    {
//        transforms.AddRequestTransform(async context =>
//        {
//            var token = await context.HttpContext.GetTokenAsync("access_token");
//            Console.WriteLine($"************** YARP Reverse Proxy - Transform - Access Token: *************** \n{token}");
//            if (!string.IsNullOrEmpty(token))
//            {
//                context.ProxyRequest.Headers.Authorization =
//                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
//            }
//        });
//    });

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.UseAuthorization();

//// GLOBAL RATE LIMITING MIDDLEWARE
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers().RequireRateLimiting("BasicRateLimitingPolicy");
//});

app.MapControllers();

app.MapReverseProxy();  // .RequireAuthorization();  // this would require authentication for all proxied requests, which is not desired - need to add authentication to specific endpoints 

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapReverseProxy();
//});

app.Run();