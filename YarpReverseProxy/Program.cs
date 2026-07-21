using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using YarpReverseProxy.CustomHttpHandlers;

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
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "YarpAzureAd", subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);
/// Configure a custom validator to prevent audience validation if have multiple downstream APIs with different audiences 
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

/// Register an IForwarderHttpClientFactory implementation to replace the default factory in order to inject a custom delegating handler
/// that applies Polly policies for resiliency (retry and circuit breaker) to outgoing requests from the reverse proxy to downstream services.
/// NOTE: Moved into the AddReverseProxy() call to ensure the custom factory is used by the reverse proxy HttpMessageInvoker instances. 
// builder.Services.AddSingleton<IForwarderHttpClientFactory, CustomHttpClientFactory>();

/// Get the downstream service URLs from configuration
string productsReadServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7101" : builder.Configuration["YARP_PRODUCTS_READ_SERVICE_URL"] ?? throw new InvalidOperationException("Products Read Service URL is not configured.");
string productsWriteServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7213" : builder.Configuration["YARP_PRODUCTS_WRITE_SERVICE_URL"] ?? throw new InvalidOperationException("Products Write Service URL is not configured.");
string accountsServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7033" : builder.Configuration["YARP_ACCOUNTS_SERVICE_URL"] ?? throw new InvalidOperationException("Accounts Service URL is not configured.");
string cartsServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7184" : builder.Configuration["YARP_CARTS_SERVICE_URL"] ?? throw new InvalidOperationException("Carts Service URL is not configured.");
string ordersServiceUrl = builder.Environment.IsDevelopment() ? "https://localhost:7019" : builder.Configuration["YARP_ORDERS_SERVICE_URL"] ?? throw new InvalidOperationException("Orders Service URL is not configured.");

/// Add Reverse Proxy
/// Retry attempts = 5 at Math.Pow(2) seconds. Circuit breaker open after 5 consecutive failures, break for 30 seconds. 
builder.Services.AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters(productsReadServiceUrl, productsWriteServiceUrl, accountsServiceUrl, cartsServiceUrl, ordersServiceUrl))
    .Services.AddSingleton<IForwarderHttpClientFactory, CustomHttpClientFactory>();

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
            RateLimiterPolicy = "BasicRateLimitingPolicy"
        },
        new RouteConfig
        {
            RouteId = "productsDevTestRoute",
            ClusterId = "products",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/dev/products/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy"
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
            RateLimiterPolicy = "BasicRateLimitingPolicy"
        },
        new RouteConfig
        {
            RouteId = "productsManagementDevTestRoute",
            ClusterId = "productsManagement",
            CorsPolicy = "PawnshopCorsPolicy",
            Match = new RouteMatch
            {
                Path = "/dev/productsManagement/{**catch-all}"
            },
            RateLimiterPolicy = "BasicRateLimitingPolicy"
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
            RateLimiterPolicy = "BasicRateLimitingPolicy"
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
            RateLimiterPolicy = "BasicRateLimitingPolicy"
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
            RateLimiterPolicy = "BasicRateLimitingPolicy"
        }
    };
}

static ClusterConfig[] GetClusters(
    string productsReadServiceUrl, string productsWriteServiceUrl, string accountsServiceUrl, string cartsServiceUrl, string ordersServiceUrl)
{
    // Note: Active HealthChecks are currently disabled avoid Azure charges for a continuously running SQL Server database
    return new[]
    {
        new ClusterConfig
        {
            ClusterId = "products",
            //// HealthChecks disabled to minimize SQL Server costs
            //HealthCheck = new HealthCheckConfig
            //{
            //    Active = new ActiveHealthCheckConfig
            //    {
            //        Enabled = true,
            //        Interval = TimeSpan.FromSeconds(30),
            //        Timeout = TimeSpan.FromSeconds(10),
            //        Policy = "ConsecutiveFailures",
            //        Path = "/api/products/healthYarp"
            //    }
            //},
            //Metadata = new Dictionary<string, string>
            //{
            //    ["ConsecutiveFailuresHealthPolicy.Threshold"] = "3"
            //},
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
            //// HealthChecks disabled to minimize SQL Server costs
            //HealthCheck = new HealthCheckConfig
            //{
            //    Active = new ActiveHealthCheckConfig
            //    {
            //        Enabled = true,
            //        Interval = TimeSpan.FromSeconds(30),
            //        Timeout = TimeSpan.FromSeconds(10),
            //        Policy = "ConsecutiveFailures",
            //        Path = "/api/productsManagement/healthYarp"
            //    }
            //},
            //Metadata = new Dictionary<string, string>
            //{
            //    ["ConsecutiveFailuresHealthPolicy.Threshold"] = "3"
            //},
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

//// DEFINE RATE LIMITING POLICIES 
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

// builder.Services.AddControllers();

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

// app.MapControllers();

app.MapGet("/", () => "YARP Reverse Proxy is up and running! Last Build: 21 Jul 2026 @ 1600 CST");

app.MapReverseProxy();  // .RequireAuthorization(); would require authentication for all proxied requests

app.Run();