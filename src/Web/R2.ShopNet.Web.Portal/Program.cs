using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using R2.ShopNet.Web.Portal.Components;
using R2.ShopNet.Web.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add sidebar service
builder.Services.AddScoped<SidebarService>();
builder.Services.AddScoped<ThemeService>();

// Configure authentication
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "R2.ShopNet.Portal";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    // Identity Server settings
    options.Authority = builder.Configuration["Authentication:Authority"] ?? "https://localhost:5003";
    options.ClientId = builder.Configuration["Authentication:ClientId"] ?? "blazor-portal";
    options.ClientSecret = builder.Configuration["Authentication:ClientSecret"] ?? "portal-secret-change-in-production";

    options.ResponseType = OpenIdConnectResponseType.Code;
    options.ResponseMode = OpenIdConnectResponseMode.Query;

    options.SaveTokens = true;
    options.RequireHttpsMetadata = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // Sign-out configuration
    options.SignedOutRedirectUri = "/";
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    // Scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("roles");

    // PKCE
    options.UsePkce = true;

    // Claim mapping
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "role";

    // For development: accept self-signed certificates
    if (builder.Environment.IsDevelopment())
    {
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }

    // Events for debugging
    options.Events = new OpenIdConnectEvents
    {
        OnAuthenticationFailed = context =>
        {
            context.Response.Redirect("/error");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            context.Response.Redirect("/error");
            context.HandleResponse();
            return Task.CompletedTask;
        },
        OnSignedOutCallbackRedirect = context =>
        {
            // After signout callback, redirect to home page without challenging
            // This prevents the automatic re-authentication loop
            context.Response.Redirect(context.Options.SignedOutRedirectUri);
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();


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
