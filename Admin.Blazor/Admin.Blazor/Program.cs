using Admin.Blazor;
using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.DTOs.Carts;
using Admin.Blazor.Client.Mappers;
using Admin.Blazor.Client.Services;
using Admin.Blazor.Client.Utility;
using Admin.Blazor.Components;
using Admin.Blazor.DownstreamApiServices;
using Admin.Blazor.HttpServices;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;
using Polly;
using Polly.Extensions.Http;
using Serilog.Sinks.Console.LogThemes;
using System.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure Persisting Auth State
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

/// MS ENTRA ID AUTH CONFIG START
// Configure authentication to use Microsoft Entra ID
// JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

string yarpProxyBaseUrl = builder.Environment.IsDevelopment() ? 
    StaticData.ProductsApiServices_LocalYarpProxyBaseURL : 
    builder.Configuration["AZURE_YARP_PROXY_BASE_URL"] ??
    throw new ArgumentNullException("YARP Proxy Base URL is not configured.");

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(msIdentityOptions =>
    {
        /// see: http s://learn.microsoft.com/en-us/entra/identity-platform/msal-client-application-configuration#:~:text=The%20authority%20is%20a%20U

        /// NOTE: If configure 'Authority' when using Microsoft.Identity.Web, the 'Instance', 'TenantId', and 'Domain' options are ignored. 
        /// So, to use those options, do not configure 'Authority' and instead configure 'Instance', 'TenantId', and 'Domain' as shown below.

        // msIdentityOptions.Authority = "http s://login.microsoftonline.com/2fd80906-88f0-4874-8d94-1d87e82053f7/v2.0";
        msIdentityOptions.Instance = "https://login.microsoftonline.com/";
        msIdentityOptions.TenantId = builder.Configuration["AZURE_CREDENTIALS_TENANT_ID"];                // for azure deploy
        msIdentityOptions.Domain = builder.Configuration["AZURE_CREDENTIALS_DOMAIN"];                    // for azure deploy

        msIdentityOptions.CallbackPath = "/signin-oidc";                        // for local development
        // msIdentityOptions.CallbackPath = "/.auth/login/aad/callback";        // for azure hosted

        msIdentityOptions.SignedOutCallbackPath = "/signout-callback-oidc";             // for local development
        // msIdentityOptions.SignedOutCallbackPath = "/.auth/logout/aad/callback";      // for azure hosted

        msIdentityOptions.ClientId = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];                // for azure deploy

        msIdentityOptions.ClientSecret = builder.Configuration["AZURE_CREDENTIALS_CLIENT_SECRET"];        // for azure deploy      

        /// warn: Microsoft.Identity.Web.MergedOptions[500]
        /// [MsIdWeb] Authority 'https://login.microsoftonline.com/2fd80906-88f0-4874-8d94-1d87e82053f7/v2.0' is being ignored because Instance 'https://login.microsoftonline.com/' and / or TenantId '2fd80906-88f0-4874-8d94-1d87e82053f7' are already configured.To use Authority, remove Instance and TenantId from the configuration.

        msIdentityOptions.ResponseType = "code";
        msIdentityOptions.GetClaimsFromUserInfoEndpoint = true;
        msIdentityOptions.MapInboundClaims = false;
        msIdentityOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
        msIdentityOptions.TokenValidationParameters.RoleClaimType = "roles";
        msIdentityOptions.SaveTokens = true;
        msIdentityOptions.AccessDeniedPath = new PathString("/AccessDenied");
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi(StaticData.AccountsApiService_ServiceName, configOptions =>                   // api name
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        // configOptions.BaseUrl = StaticData.AccountsApiService_LocalBaseURL;                      // api base url
        configOptions.Scopes = [builder.Configuration["ACCOUNTS_API_SCOPE"]!];                      // Note: scope shows in api access token, not client identity token
    })
    .AddDownstreamApi(StaticData.CartsApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        // configOptions.BaseUrl = StaticData.CartsApiService_LocalBaseURL;
        configOptions.Scopes = [builder.Configuration["CARTS_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.OrdersApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        // configOptions.BaseUrl = StaticData.OrdersApiService_LocalBaseURL;
        configOptions.Scopes = [builder.Configuration["ORDERS_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.ProductsReadApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        // configOptions.BaseUrl = StaticData.ProductsReadApiService_LocalBaseURL;
        configOptions.Scopes = [builder.Configuration["PRODUCTS_READ_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.ProductsWriteApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        // configOptions.BaseUrl = StaticData.ProductsWriteApiService_LocalBaseURL;
        configOptions.Scopes = [builder.Configuration["PRODUCTS_WRITE_API_SCOPE"]!];
    })
    .AddDistributedTokenCaches();

builder.Services.AddDistributedMemoryCache();

builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(
    options =>
    {
        //options.DisableL1Cache = false;                           // Disable L1 Cache default: false
        //options.L1CacheOptions.SizeLimit = 500 * 1024 * 1024;     // L1 Cache Size Limit default: 500 MB
        options.Encrypt = true;                                     // Encrypt tokens at rest default: false
        //options.SlidingExpiration = TimeSpan.FromHours(1);        // Sliding Expiration default: 1 hour
    });
/// MS ENTRA ID AUTH CONFIG END

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsAdmin", policy => policy.RequireClaim("roles", "Admin"));                          
    options.AddPolicy("IsManager", policy => policy.RequireClaim("role", "Manager"));                      
    options.AddPolicy("IsAdminOrManager", policy => policy.RequireClaim("roles", "Admin", "Manager"));      
    options.AddPolicy("MarlowAndWendy", policy => policy.RequireClaim(ClaimTypes.Name, "Wendy Davenport", "Marlow Bean"));
    options.AddPolicy("DomesticDogs", policy => policy.RequireClaim("Genus", "Canis").RequireClaim("Species", "Familiaris"));
    // testing
    options.AddPolicy("NoOneHasThisPolicy", policy => policy.RequireClaim("roles", "NoOneHasBeenAssignedThisRole"));
});

// Downstream API Services
builder.Services.AddScoped<IProductsReadHttpService, ProductsReadApiService>();
builder.Services.AddScoped<IProductsWriteHttpService, ProductsWriteApiService>();
builder.Services.AddScoped<ICartsHttpService, CartsApiService>();
builder.Services.AddScoped<IAccountsHttpService, AccountsApiService>();
builder.Services.AddScoped<IOrdersHttpService, OrdersApiService>();
builder.Services.AddScoped<IClaimsHttpService, ClaimsApiService>();

// HTTP Clients - for using HttpClientFactory to create HttpClients to call API Resources without the user being logged in
builder.Services.AddHttpClient(name: StaticData.ProductsReadHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(yarpProxyBaseUrl);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});  // PUBLIC HTTP CLIENT - NOT CONFIGURED TO PASS TOKENS  // .AddUserAccessTokenHandler(); 
builder.Services.AddSingleton<IPublicProductsReadHttpService, ProductsReadHttpService>();

//// Http Client for checking status of or waking deployed API and database resources 
builder.Services.AddHttpClient(name: StaticData.AzureServicesHttpClient_ClientName)
    .AddPolicyHandler(GetRetryPolicy());
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
builder.Services.AddSingleton<IAzureServicesHttpClient, AzureServicesHttpClient>();

// Services
builder.Services.AddScoped<IOrderMapper, OrderMapper>();
builder.Services.AddScoped<IToastrService, ToastrService>();

// Root Level Cascading Value Source for Cart Data provides access to underlying source's NotifyChangedAsync() method
builder.Services.AddSingleton(sp =>
{
    var cartData = new CartData { ItemCount = -1, ShowCart = false };
    return new CascadingValueSource<CartData>(name: "ShoppingCartData", cartData, isFixed: false);
});
// Root Level Cascading Value that will be the cascading parameter property deriving from the source 
builder.Services.AddCascadingValue(sp => sp.GetRequiredService<CascadingValueSource<CartData>>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Admin.Blazor.Client._Imports).Assembly);

app.MapGet("/ping", () => "Blazor Admin Client is up and running!  Last Build: 22 Jul 2026 @ 1445 CST").AllowAnonymous();

app.MapGet("/login", (string? returnUrl, HttpContext httpContext) =>
{
    Console.WriteLine($"Blazor Web App logging in ...");
    // ensure the returnUrl is valid & safe (calls method ValidateUri defined int partial Program class below):
    // - needs to be a local url relative to host in relative or absolute uri kind
    // - avoids open redirect attacks by accepting only redirects local to current host
    // - see https://learn.microsoft.com/en-us/aspnet/core/security/preventing-open-redirects?view=aspnetcore-8.0

    returnUrl = ValidateUri(httpContext, returnUrl);        // see method definition below

    // start oidc flow by challenging default scheme set in Authentication config
    return TypedResults.Challenge(
                 new AuthenticationProperties
                 { RedirectUri = returnUrl });
}).AllowAnonymous();    					                // requried for login

app.MapPost("/logout", async ([FromForm] string? returnUrl, HttpContext httpContext) =>
{
    var accessToken = await httpContext.GetTokenAsync("access_token");
    if (string.IsNullOrEmpty(accessToken)) Console.WriteLine($"ACCESS TOKEN FROM LOGOUT WAS NULL");
    else Console.WriteLine($"ACCESS TOKEN FROM LOGOUT: \n{accessToken}");

    returnUrl = ValidateUri(httpContext, returnUrl);

    // sign out of local scheme to clear local cookie, and
    // sign out of EntraID to trigger redirect to EntraID so it can clear it's own cookie
    return TypedResults.SignOut(
        new AuthenticationProperties
        { RedirectUri = returnUrl },
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
});

// app.MapControllers();

app.Run();

public partial class Program
{
    private static string ValidateUri(HttpContext httpContext, string? uri)
    {
        string basePath = string.IsNullOrEmpty(httpContext.Request.PathBase)
                ? "/" : httpContext.Request.PathBase;

        if (string.IsNullOrEmpty(uri))
        {
            return basePath;
        }
        else if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            return new Uri(uri, UriKind.Absolute).PathAndQuery;
        }
        else if (uri[0] != '/')
        {
            return $"{basePath}{uri}";
        }

        return uri;
    }
}

