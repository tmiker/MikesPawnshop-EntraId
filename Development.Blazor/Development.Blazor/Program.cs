using Development.Blazor;
using Development.Blazor.Abstractions;
using Development.Blazor.Client.Abstractions;
using Development.Blazor.Client.Utility;
using Development.Blazor.Components;
using Development.Blazor.HttpProviders;
using Development.Blazor.Mappers;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add controllers for client proxy services
builder.Services.AddControllers();
// Configure Persisting Auth State
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();
// Duende Access Token Management
builder.Services.AddDistributedMemoryCache();   // to store tokens
builder.Services.AddOpenIdConnectAccessTokenManagement();   // decorate http client with handler
// Configure Auth
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;    // duendeIdScheme;
}).AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions => // (duendeIdScheme, oidcOptions =>
{
    oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    oidcOptions.Authority = "https://localhost:5001";
    oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
    oidcOptions.UsePkce = true;
    oidcOptions.ClientId = "devTestBlazorServer";
    oidcOptions.ClientSecret = builder.Configuration["AuthenticationSettings:IdentityProviderClientSecret"];
    oidcOptions.Scope.Add(OpenIdConnectScope.OpenIdProfile);
    // oidcOptions.Scope.Add(OpenIdConnectScope.OfflineAccess);
    oidcOptions.Scope.Add("roles");
    oidcOptions.Scope.Add("cartsapi.fullaccess");
    oidcOptions.Scope.Add("productsreadapi.fullaccess");
    oidcOptions.Scope.Add("productswriteapi.fullaccess");
    oidcOptions.Scope.Add("accountsapi.fullaccess");
    oidcOptions.Scope.Add("ordersapi.fullaccess");
    oidcOptions.CallbackPath = new PathString("/signin-oidc");
    oidcOptions.SignedOutCallbackPath = new PathString("/signout-callback-oidc");
    oidcOptions.GetClaimsFromUserInfoEndpoint = true;
    oidcOptions.MapInboundClaims = false;
    // Mapped claim args are claim type in incoming token, claim type in users claims list
    oidcOptions.ClaimActions.MapJsonKey("role", "role");    // can have more than one claim of the type
    // oidcOptions.ClaimActions.MapUniqueJsonKey("employeeId", "employeeId");  // if single instance of claim type
    oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
    oidcOptions.TokenValidationParameters.RoleClaimType = "role";
    oidcOptions.SaveTokens = true;
    // oidcOptions.EventsType = typeof(CustomTokenStorageOidcEvents);

}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.AccessDeniedPath = "/AccessDenied";
});

//builder.Services.AddAuthorization();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsAdmin", policy => policy.RequireClaim("roles", "Admin"));                          // (ClaimTypes.Role, "Admin")); does not work
    options.AddPolicy("IsManager", policy => policy.RequireClaim("roles", "Manager"));                      // (ClaimTypes.Role, "Manager")); does not work
    options.AddPolicy("IsAdminOrManager", policy => policy.RequireClaim("roles", "Admin", "Manager"));      // (ClaimTypes.Role, "Admin", "Manager"));does not work
    options.AddPolicy("MarlowAndWendy", policy => policy.RequireClaim(ClaimTypes.Name, "Wendy Davenport", "Marlow Bean"));
    options.AddPolicy("DomesticDogs", policy => policy.RequireClaim("Genus", "Canis").RequireClaim("Species", "Familiaris"));
});

builder.Services.AddScoped<IOrderMapper, OrderMapper>();

// Http Clients
builder.Services.AddHttpClient(name: StaticData.ProductsReadHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(StaticData.ProductsReadHttpClient_BaseURL);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddUserAccessTokenHandler();
builder.Services.AddSingleton<IProductsReadHttpService, ProductsReadHttpService>();

builder.Services.AddHttpClient(name: StaticData.ProductsWriteHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(StaticData.ProductsWriteHttpClient_BaseURL);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddUserAccessTokenHandler();
builder.Services.AddSingleton<IProductsWriteHttpService, ProductsWriteHttpService>();
builder.Services.AddHttpClient(name: StaticData.CartsHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(StaticData.CartsHttpClient_BaseURL);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddUserAccessTokenHandler();
builder.Services.AddSingleton<ICartsHttpService, CartsHttpService>();
builder.Services.AddHttpClient(name: StaticData.AccountsHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(StaticData.AccountsHttpClient_BaseURL);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddUserAccessTokenHandler();
builder.Services.AddSingleton<IAccountsHttpService, AccountsHttpService>();
builder.Services.AddHttpClient(name: StaticData.OrdersHttpClient_ClientName, configureClient: config =>
{
    config.BaseAddress = new Uri(StaticData.OrdersHttpClient_BaseURL);
    config.DefaultRequestHeaders.Clear();
    config.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).AddUserAccessTokenHandler();
builder.Services.AddSingleton<IOrdersHttpService, OrdersHttpService>();

    
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
    .AddAdditionalAssemblies(typeof(Development.Blazor.Client._Imports).Assembly);

//// CLIENT HTTP PROXY SERVICE ENDPOINTS - Moved to dedicated Api Controllers in HttpProxyServices folder
//app.MapGet("/localapi/cart/getApiUserInfo", async (ICartHttpService serverCartService) =>
//{
//    var result = await serverCartService.GetCartsApiUserInfoAsync();
//    return Results.Ok(result.ApiUserInfo);
//}).RequireAuthorization();


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

app.MapControllers();

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