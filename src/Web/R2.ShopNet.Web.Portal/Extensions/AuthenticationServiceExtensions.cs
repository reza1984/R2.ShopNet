using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;

namespace R2.ShopNet.Web.Portal.Extensions;

/// <summary>
/// Extension methods for configuring authentication services
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Adds OpenID Connect authentication with cookie authentication
    /// </summary>
    public static IServiceCollection AddPortalAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Configure JWT token handling
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services.AddAuthentication(options =>
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
            ConfigureOpenIdConnect(options, configuration, environment);
        });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Identity Server settings
        options.Authority = configuration["Authentication:Authority"] ?? "https://localhost:5003";
        options.ClientId = configuration["Authentication:ClientId"] ?? "blazor-portal";
        options.ClientSecret = configuration["Authentication:ClientSecret"] ?? "portal-secret-change-in-production";

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
        if (environment.IsDevelopment())
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        // Events for error handling
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
    }
}
