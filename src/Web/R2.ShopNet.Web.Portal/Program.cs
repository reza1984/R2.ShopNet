using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using R2.ShopNet.Web.Portal.Components;
using R2.ShopNet.Web.Portal.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add portal services (theme, sidebar, navigation, JS interop)
builder.Services.AddPortalServices();

// Configure authentication (OpenID Connect + Cookies)
builder.Services.AddPortalAuthentication(builder.Configuration, builder.Environment);


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(R2.ShopNet.Web.Portal.Client._Imports).Assembly);

// Add logout endpoint
app.MapGet("/logout", async (HttpContext context) =>
{
    // Sign out will redirect to the OIDC provider's logout endpoint
    // and then back to the post logout redirect URI
    return Results.SignOut(
        authenticationSchemes: new[] { CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme },
        properties: new AuthenticationProperties
        {
            RedirectUri = "/"
        }
    );
});

app.Run();
