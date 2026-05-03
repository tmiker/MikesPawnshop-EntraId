using Consumer.Blazor.Client;
using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.Services;
using Consumer.Blazor.Client.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var environment = builder.HostEnvironment.Environment;
var baseAddress = environment == "Development"
    ? StaticData.WasmClient_LocalApiBaseAddress ?? throw new Exception("LocalConsumerWasmClient BaseAddress is missing.")
    : StaticData.WasmClient_AzureApiBaseAddress ?? throw new Exception("AzureConsumerWasmClient BaseAddress is missing.");

    builder.Services.AddKeyedScoped<HttpClient>("LocalConsumerWasmClient",
    (sp, key) =>
       new HttpClient
       {
           BaseAddress = new Uri(baseAddress ?? throw new Exception("Wasm Client BaseAddress is missing."))
       });

builder.Services.AddScoped<IToastrService, ToastrService>();

builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
