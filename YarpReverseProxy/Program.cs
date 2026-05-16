using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    // add origins for Consumer and Admin Blazor clients
    options.AddPolicy("PawnshopCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment()) policy.WithOrigins(["https://localhost:7088", "https://localhost:7217"]);
        else policy.WithOrigins(
            ["https://pawnshopconsumer-dtaugcdmfrfvbygz.centralus-01.azurewebsites.net",
             "https://pawnshopadmin-cfd2asdcc2eceeac.centralus-01.azurewebsites.net"]);

        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Pagination");
    });
});

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
IdentityModelEventSource.ShowPII = true;  // Enable detailed error messages for token validation issues (useful for debugging, but should be disabled in production)
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;  // Prevent automatic mapping of standard JWT claims to Microsoft-specific claim types (e.g. "sub" to "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")

/// ADD JWT BEARER AUTHENTICATION WITH MICROSOFT IDENTITY WEB API
/// OPTION 1: Use Microsoft.Identity.Web's built-in extension method for adding JWT Bearer authentication, 
/// which simplifies configuration by automatically binding settings from configuration (e.g. appsettings.json) 
/// and provides additional features like automatic token validation and integration with Azure AD.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "YarpAzureAd", subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);
/// If use Option 1, configure a custom validator to prevent audience validation if have multiple downstream APIs with different audiences 
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false, // Skip audience validation
        ValidIssuer = builder.Configuration["YARP_AZURE_CREDENTIALS_AUTHORITY"],  
        ValidateIssuer = true
        /// configure other settings as desired ...
        // ValidateIssuerSigningKey = true,
        // ValidateLifetime = true
    };
});
/// OPTION 2: Manually configure JWT Bearer authentication, which provides more control over the configuration but 
/// requires more code and careful handling of token validation parameters.
/// If use this option, make sure to set TokenValidationParameters.ValidateAudience to false and ADD TRANSFORM TO PASS THE TOKEN in AddReverseProxy() below
//builder.Services.AddAuthentication("Bearer")
//.AddJwtBearer("Bearer", options =>
//{
//    options.Authority = builder.Configuration["AZURE_CREDENTIALS_AUTHORITY"];       //  "https://login.microsoftonline.com/{tenantId}/v2.0";
//    // options.Audience = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];        // "{clientId}";
//    // options.Audience = "https://localhost:7245";
//    options.TokenValidationParameters.ValidateAudience = false;
//});

//// ADD YARP REVERSE PROXY
/// OPTION 1: Configure in appsettings and/or secrets
//builder.Services.AddReverseProxy()
//    .LoadFromConfig(builder.Configuration.GetRequiredSection("YarpProxySettings"));
////.AddTransforms(transforms =>
////{
////    transforms.AddRequestTransform(async context =>
////    {
////        if (context.HttpContext.User.Identity is not null && context.HttpContext.User.Identity.IsAuthenticated)
////        {
////            // Extract the JWT token from the incoming request
////            var token = await context.HttpContext.GetTokenAsync("access_token");

////            // Add the token to the outgoing request headers
////            if (!string.IsNullOrEmpty(token))
////            {
////                context.ProxyRequest.Headers.Authorization =
////                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
////            }
////        }
////    });
////});
/// OPTION 2: Configure in code
string productsReadServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7101" : builder.Configuration["YARP_PRODUCTS_READ_SERVICE_URL"] ?? throw new InvalidOperationException("Products Read Service URL is not configured.");
string productsWriteServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7213" : builder.Configuration["YARP_PRODUCTS_WRITE_SERVICE_URL"] ?? throw new InvalidOperationException("Products Write Service URL is not configured.");
string accountsServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7033" : builder.Configuration["YARP_ACCOUNTS_SERVICE_URL"] ?? throw new InvalidOperationException("Accounts Service URL is not configured.");
string cartsServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7184" : builder.Configuration["YARP_CARTS_SERVICE_URL"] ?? throw new InvalidOperationException("Carts Service URL is not configured.");
string ordersServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7019" : builder.Configuration["YARP_ORDERS_SERVICE_URL"] ?? throw new InvalidOperationException("Orders Service URL is not configured.");

builder.Services.AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters(productsReadServiceUrl, productsWriteServiceUrl, accountsServiceUrl, cartsServiceUrl, ordersServiceUrl));

static RouteConfig[] GetRoutes()
{
    return new[]
    {
        new RouteConfig
        {
            RouteId = "productsRoute",
            ClusterId = "products",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/api/products/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy" //,
            // TimeoutPolicy = "Default"
        },
        new RouteConfig
        {
            RouteId = "productsManagementRoute",
            ClusterId = "productsManagement",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/api/productsManagement/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy" //,
            // TimeoutPolicy = "Default"
        },
        new RouteConfig
        {
            RouteId = "accountsRoute",
            ClusterId = "accounts",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/api/accounts/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy" //,
            // TimeoutPolicy = "Default"
        },
        new RouteConfig
        {
            RouteId = "cartsRoute",
            ClusterId = "carts",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/api/carts/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy" //,
            // TimeoutPolicy = "Default"
        },
        new RouteConfig
        {
            RouteId = "ordersRoute",
            ClusterId = "orders",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/api/orders/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy" //,
            // TimeoutPolicy = "Default"
        }
    };
}

static ClusterConfig[] GetClusters(
    string productsReadServiceUrl, string productsWriteServiceUrl, string accountsServiceUrl, string cartsServiceUrl, string ordersServiceUrl)
{
    return new[]
    {
        new ClusterConfig
        {
            ClusterId = "products",
            HealthCheck = new HealthCheckConfig
            {
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = TimeSpan.FromSeconds(30),
                    Timeout = TimeSpan.FromSeconds(10),
                    Policy = "ConsecutiveFailures",
                    Path = "/api/products/healthYarp"
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["ConsecutiveFailuresHealthPolicy.Threshold"] = "3"
            },
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["productsReadService"] = new DestinationConfig
                {
                    Address = productsReadServiceUrl
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "productsManagement",
            HealthCheck = new HealthCheckConfig
            {
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = TimeSpan.FromSeconds(30),
                    Timeout = TimeSpan.FromSeconds(10),
                    Policy = "ConsecutiveFailures",
                    Path = "/api/productsManagement/healthYarp"
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["ConsecutiveFailuresHealthPolicy.Threshold"] = "3"
            },
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["productsWriteService"] = new DestinationConfig
                {
                    Address = productsWriteServiceUrl
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "accounts",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["accountsService"] = new DestinationConfig
                {
                    Address = accountsServiceUrl
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "carts",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["cartsService"] = new DestinationConfig
                {
                    Address = cartsServiceUrl
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "orders",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["ordersService"] = new DestinationConfig
                {
                    Address = ordersServiceUrl
                }
            }
        }
    };
}

builder.Services.AddAuthorization();

//// ADD RATE LIMITING POLICIES 
/// Rate Limiting Policies can be global or named - named are specified by endpoint or page
//// OPTION 1. Global Rate Limiting Policy that permits 10 requests per minute by user (identity) or globally:
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
// OPTION2. Named Rate Limiting Policy to be added to specific endpoints or globally (see yarp_notes.txt):
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

app.UseRouting();

app.UseCors("PawnshopCorsPolicy");      // After routing, before auth

app.UseAuthentication();
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