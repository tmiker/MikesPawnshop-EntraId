using Admin.Blazor.Client;
using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.Services;
using Admin.Blazor.Client.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var environment = builder.HostEnvironment.Environment;
var baseAddress = environment == "Development"
    ? StaticData.WasmClient_LocalApiBaseAddress ?? throw new Exception("LocalAdminWasmClient BaseAddress is missing.")
    : StaticData.WasmClient_AzureApiBaseAddress ?? throw new Exception("AzureAdminWasmClient BaseAddress is missing.");

builder.Services.AddKeyedScoped<HttpClient>("LocalAdminWasmClient",
    (sp, key) =>
       new HttpClient
       {
           BaseAddress = new Uri(baseAddress ??                    
                throw new Exception("LocalAdminWasmClient BaseAddress is missing."))
       });

builder.Services.AddScoped<IToastrService, ToastrService>();

builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
