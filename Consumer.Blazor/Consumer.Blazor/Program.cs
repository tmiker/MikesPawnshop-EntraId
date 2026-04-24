using Consumer.Blazor;
using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.Mappers;
using Consumer.Blazor.Client.Services;
using Consumer.Blazor.Client.Utility;
using Consumer.Blazor.Components;
using Consumer.Blazor.DownstreamApiServices;
using Consumer.Blazor.HttpServices;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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


/// DUENDE OIDC AUTH CONFIG START
////// Duende Access Token Management
//builder.Services.AddDistributedMemoryCache();   // to store tokens
//builder.Services.AddOpenIdConnectAccessTokenManagement();   // decorate http client with handler

////// Configure Auth

//JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;    
//}).AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions => 
//{
//    oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    oidcOptions.Authority = "https://localhost:5001";
//    oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
//    oidcOptions.UsePkce = true;
//    oidcOptions.ClientId = "consumerBlazorServer";
//    oidcOptions.ClientSecret = builder.Configuration["AuthenticationSettings:IdentityProviderClientSecret"];
//    oidcOptions.Scope.Add(OpenIdConnectScope.OpenIdProfile);
//    // oidcOptions.Scope.Add(OpenIdConnectScope.OfflineAccess);
//    oidcOptions.Scope.Add("roles");
//    oidcOptions.Scope.Add("cartsapi.fullaccess");
//    oidcOptions.Scope.Add("productsreadapi.fullaccess");
//    // oidcOptions.Scope.Add("productswriteapi.fullaccess");
//    oidcOptions.Scope.Add("accountsapi.fullaccess");
//    oidcOptions.Scope.Add("ordersapi.fullaccess");
//    oidcOptions.CallbackPath = new PathString("/signin-oidc");
//    oidcOptions.SignedOutCallbackPath = new PathString("/signout-callback-oidc");
//    // oidcOptions.SignedOutRedirectUri = "https://localhost:7217/";
//    oidcOptions.GetClaimsFromUserInfoEndpoint = true;
//    oidcOptions.MapInboundClaims = false;
//    // Mapped claim args are claim type in incoming token, claim type in users claims list
//    oidcOptions.ClaimActions.MapJsonKey("role", "role");    // can have more than one claim of the type
//    // oidcOptions.ClaimActions.MapUniqueJsonKey("employeeId", "employeeId");  // if single instance of claim type
//    oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
//    oidcOptions.TokenValidationParameters.RoleClaimType = "role";
//    oidcOptions.SaveTokens = true;
//    // oidcOptions.EventsType = typeof(CustomTokenStorageOidcEvents);

//}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
//{
//    options.AccessDeniedPath = "/AccessDenied";
//});
/// DUENDE OIDC AUTH CONFIG START

/// MS ENTRA ID AUTH CONFIG START

// Configure authentication to use Microsoft Entra ID
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(msIdentityOptions =>
    {
        msIdentityOptions.Authority = "https://login.microsoftonline.com/2fd80906-88f0-4874-8d94-1d87e82053f7/v2.0";

        msIdentityOptions.CallbackPath = "/signin-oidc";                        // for local development
        //// msIdentityOptions.CallbackPath = "https://ubiquitous-hudta7feejabdyew.centralus-01.azurewebsites.net/.auth/login/aad/callback";    // wasn't used
        // msIdentityOptions.CallbackPath = "/.auth/login/aad/callback";        // for azure hosted


        msIdentityOptions.SignedOutCallbackPath = "/signout-callback-oidc";             // for local development
        //// msIdentityOptions.SignedOutCallbackPath = "https://ubiquitous-hudta7feejabdyew.centralus-01.azurewebsites.net/.auth/logout/aad/callback";      // wasn't used
        // msIdentityOptions.SignedOutCallbackPath = "/.auth/logout/aad/callback";      // for azure hosted

        // msIdentityOptions.ClientId = builder.Configuration["MicrosoftIdentity:ClientId"];                   // Application (client) ID for this blazor app";
        msIdentityOptions.ClientId = builder.Configuration["AZURE_CREDENTIALS_CLIENT_ID"];                // for azure deploy

        // msIdentityOptions.ClientSecret = builder.Configuration["MicrosoftIdentity:ClientSecret"];
        msIdentityOptions.ClientSecret = builder.Configuration["AZURE_CREDENTIALS_CLIENT_SECRET"];        // for azure deploy

        // msIdentityOptions.Domain = builder.Configuration["MicrosoftIdentity:Domain"];                       //  { DIRECTORY (tenant) NAME}.onmicrosoft.com";
        msIdentityOptions.Domain = builder.Configuration["AZURE_CREDENTIALS_DOMAIN"];                    // for azure deploy

        msIdentityOptions.Instance = "https://login.microsoftonline.com/";
        msIdentityOptions.ResponseType = "code";

        // msIdentityOptions.TenantId = builder.Configuration["MicrosoftIdentity:TenantId"];
        msIdentityOptions.TenantId = builder.Configuration["AZURE_CREDENTIALS_TENANT_ID"];                // for azure deploy

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
        configOptions.BaseUrl = StaticData.AccountsApiService_LocalBaseURL;                    // api base url
        // configOptions.BaseUrl = StaticData.AccountsApiService_AzureBaseURL;
        configOptions.Scopes = [builder.Configuration["ACCOUNTS_API_SCOPE"]!];                 // Note: scope shows in api access token, not client identity token
    })
    .AddDownstreamApi(StaticData.CartsApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = StaticData.CartsApiService_LocalBaseURL;
        configOptions.Scopes = [builder.Configuration["CARTS_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.OrdersApiService_ServiceName, configOptions =>
    {
         configOptions.BaseUrl = StaticData.OrdersApiService_LocalBaseURL;
         configOptions.Scopes = [builder.Configuration["ORDERS_API_SCOPE"]!];
    })
    .AddDownstreamApi(StaticData.ProductsReadApiService_ServiceName, configOptions =>
    {
        configOptions.BaseUrl = StaticData.ProductsReadApiService_LocalBaseURL;
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
    options.AddPolicy("IsAdmin", policy => policy.RequireClaim("role", "Admin"));
    options.AddPolicy("IsManager", policy => policy.RequireClaim("role", "Manager"));
    options.AddPolicy("IsAdminOrManager", policy => policy.RequireClaim("role", "Admin", "Manager"));
    options.AddPolicy("MarlowAndWendy", policy => policy.RequireClaim(ClaimTypes.Name, "Wendy Davenport", "Marlow Bean"));
    options.AddPolicy("DomesticDogs", policy => policy.RequireClaim("Genus", "Canis").RequireClaim("Species", "Familiaris"));
});

//// Downstream API Clients
builder.Services.AddSingleton<IProductsReadHttpService, ProductsReadApiService>();
builder.Services.AddSingleton<ICartsHttpService, CartsApiService>();
builder.Services.AddSingleton<IAccountsHttpService, AccountsApiService>();
builder.Services.AddSingleton<IOrdersHttpService, OrdersApiService>();
builder.Services.AddSingleton<IClaimsHttpService, ClaimsApiService>();

//// HTTP Clients
//builder.Services.AddHttpClient(name: StaticData.ProductsReadHttpClient_ClientName, configureClient: config =>
//{
//    config.BaseAddress = new Uri(StaticData.ProductsReadHttpClient_BaseURL);
//    config.DefaultRequestHeaders.Clear();
//    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//}).AddUserAccessTokenHandler();
// builder.Services.AddSingleton<IProductsReadHttpService, ProductsReadHttpService>();
//builder.Services.AddHttpClient(name: StaticData.CartsHttpClient_ClientName, configureClient: config =>
//{
//    config.BaseAddress = new Uri(StaticData.CartsHttpClient_BaseURL);
//    config.DefaultRequestHeaders.Clear();
//    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//}).AddUserAccessTokenHandler();
// builder.Services.AddSingleton<ICartsHttpService, CartsHttpService>();
//builder.Services.AddHttpClient(name: StaticData.AccountsHttpClient_ClientName, configureClient: config =>
//{
//    config.BaseAddress = new Uri(StaticData.AccountsHttpClient_BaseURL);
//    config.DefaultRequestHeaders.Clear();
//    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//}).AddUserAccessTokenHandler();
// builder.Services.AddSingleton<IAccountsHttpService, AccountsHttpService>();
//builder.Services.AddHttpClient(name: StaticData.OrdersHttpClient_ClientName, configureClient: config =>
//{
//    config.BaseAddress = new Uri(StaticData.OrdersHttpClient_BaseURL);
//    config.DefaultRequestHeaders.Clear();
//    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//}).AddUserAccessTokenHandler();
// builder.Services.AddSingleton<IOrdersHttpService, OrdersHttpService>();
// builder.Services.AddSingleton<IClaimsHttpService, ClaimsHttpService>();

// Services
builder.Services.AddScoped<IOrderMapper, OrderMapper>();
builder.Services.AddScoped<IToastrService, ToastrService>();

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