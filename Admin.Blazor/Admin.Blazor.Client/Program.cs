using Admin.Blazor.Client;
using Admin.Blazor.Client.Abstractions;
using Admin.Blazor.Client.Services;
using Admin.Blazor.Client.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddKeyedScoped<HttpClient>("LocalAdminWasmClient",
    (sp, key) =>
       new HttpClient
       {
           BaseAddress = new Uri(StaticData.WasmClient_LocalApiBaseAddress ??                    // new Uri(builder.Configuration["LocalConsumerWasmClientBaseAddress"] ??
                throw new Exception("LocalConsumerWasmClient BaseAddress is missing."))
       });

builder.Services.AddScoped<IToastrService, ToastrService>();

builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
