using Consumer.Blazor.Client;
using Consumer.Blazor.Client.Abstractions;
using Consumer.Blazor.Client.Services;
using Consumer.Blazor.Client.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddKeyedScoped<HttpClient>("LocalConsumerWasmClient",
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
