using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using R2.ShopNet.Web.Portal.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add services for the WebAssembly client
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Add authentication state provider for WebAssembly
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

// Add HTTP client for API calls
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

await builder.Build().RunAsync();
