using Consumer.Blazor;
using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.DTOs.Carts;
using Consumer.Blazor.Client.Mappers;
using Consumer.Blazor.Client.Services;
using Consumer.Blazor.Client.Utility;
using Consumer.Blazor.Components;
using Consumer.Blazor.DownstreamApiServices;
using Consumer.Blazor.HttpServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;
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
        
        // msIdentityOptions.Authority = "https://login.microsoftonline.com/2fd80906-88f0-4874-8d94-1d87e82053f7/v2.0";     
        msIdentityOptions.Instance = "https://login.microsoftonline.com/";
        msIdentityOptions.TenantId = builder.Configuration["AZURE_CREDENTIALS_TENANT_ID"]; 
        msIdentityOptions.Domain = builder.Configuration["AZURE_CREDENTIALS_DOMAIN"]; 

        msIdentityOptions.CallbackPath = "/signin-oidc";
        // msIdentityOptions.CallbackPath = "/.auth/login/aad/callback";        // azure default

        msIdentityOptions.SignedOutCallbackPath = "/signout-callback-oidc";
        // msIdentityOptions.SignedOutCallbackPath = "/.auth/logout/aad/callback";      // azure default

        msIdentityOptions.ClientId = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];                
        msIdentityOptions.ClientSecret = builder.Configuration["AZURE_CREDENTIALS_CLIENT_SECRET"];        

        msIdentityOptions.ResponseType = "code";
        msIdentityOptions.GetClaimsFromUserInfoEndpoint = true;
        msIdentityOptions.MapInboundClaims = false;
        msIdentityOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
        msIdentityOptions.TokenValidationParameters.RoleClaimType = "roles";
        msIdentityOptions.SaveTokens = true;
        msIdentityOptions.AccessDeniedPath = new PathString("/AccessDenied");
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi(StaticData.AccountsApiService_ServiceName, configOptions =>              // api name
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        configOptions.Scopes = [builder.Configuration["ACCOUNTS_API_SCOPE"]!];                 
    })
    .AddDownstreamApi(StaticData.CartsApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        configOptions.Scopes = [builder.Configuration["CARTS_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.OrdersApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        configOptions.Scopes = [builder.Configuration["ORDERS_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.ProductsReadApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = yarpProxyBaseUrl;
        configOptions.Scopes = [builder.Configuration["PRODUCTS_READ_API_SCOPE"]!];
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
    options.AddPolicy("IsManager", policy => policy.RequireClaim("roles", "Manager"));
    options.AddPolicy("IsAdminOrManager", policy => policy.RequireClaim("roles", "Admin", "Manager"));
    options.AddPolicy("MarlowAndWendy", policy => policy.RequireClaim(ClaimTypes.Name, "Wendy Davenport", "Marlow Bean"));
    options.AddPolicy("DomesticDogs", policy => policy.RequireClaim("Genus", "Canis").RequireClaim("Species", "Familiaris"));
    // testing
    options.AddPolicy("NoOneHasThisPolicy", policy => policy.RequireClaim("roles", "NoOneHasBeenAssignedThisRole"));
});

//// Downstream API Clients
builder.Services.AddScoped<IProductsReadHttpService, ProductsReadApiService>();
builder.Services.AddScoped<ICartsHttpService, CartsApiService>();
builder.Services.AddScoped<IAccountsHttpService, AccountsApiService>();
builder.Services.AddScoped<IOrdersHttpService, OrdersApiService>();
builder.Services.AddScoped<IClaimsHttpService, ClaimsApiService>();

//// Public Access HTTP Clients (No Access Token Handler)
builder.Services.AddHttpClient(name: StaticData.ProductsReadHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(yarpProxyBaseUrl);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});  // .AddUserAccessTokenHandler();
builder.Services.AddSingleton<IPublicProductsReadHttpService, ProductsReadHttpService>();

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
    .AddAdditionalAssemblies(typeof(Consumer.Blazor.Client._Imports).Assembly);

app.MapGet("/ping", () => "Blazor Consumer Client is up and running!  Last Build: 8 Jun 2026 @ 06:45 CST").AllowAnonymous();

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

//  app.MapControllers();

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