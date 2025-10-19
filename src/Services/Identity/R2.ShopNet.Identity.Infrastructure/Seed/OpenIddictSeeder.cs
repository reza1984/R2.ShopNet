using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace R2.ShopNet.Identity.Infrastructure.Seed;

/// <summary>
/// Seeds OpenIddict clients and scopes for the application
/// </summary>
public class OpenIddictSeeder
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<OpenIddictSeeder> _logger;

    public OpenIddictSeeder(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        ILogger<OpenIddictSeeder> logger)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedScopesAsync();
        await SeedClientsAsync();
    }

    private async Task SeedScopesAsync()
    {
        // Define scopes for the application
        var scopes = new[]
        {
            new { Name = "openid", DisplayName = "OpenID", Description = "OpenID Connect scope" },
            new { Name = "profile", DisplayName = "Profile", Description = "User profile information" },
            new { Name = "email", DisplayName = "Email", Description = "User email address" },
            new { Name = "roles", DisplayName = "Roles", Description = "User roles" },
            new { Name = "api", DisplayName = "API Access", Description = "Full API access" },
            new { Name = "admin", DisplayName = "Admin Access", Description = "Administrative access" }
        };

        foreach (var scope in scopes)
        {
            // Check if scope already exists
            if (await _scopeManager.FindByNameAsync(scope.Name) is null)
            {
                await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scope.Name,
                    DisplayName = scope.DisplayName,
                    Description = scope.Description,
                    Resources =
                    {
                        "shopnet-api"  // Resource server identifier
                    }
                });

                _logger.LogInformation("Created scope: {ScopeName}", scope.Name);
            }
        }
    }

    private async Task SeedClientsAsync()
    {
        // Admin Web Application Client
        if (await _applicationManager.FindByClientIdAsync("admin-web") is null)
        {
            await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "admin-web",
                DisplayName = "R2.ShopNet Admin Web Application",
                ClientType = ClientTypes.Public, // Public client (SPA) - no client secret
                ConsentType = ConsentTypes.Implicit, // No consent required
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api",
                    Permissions.Prefixes.Scope + "admin"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            });

            _logger.LogInformation("Created client: admin-web");
        }

        // Customer Web Application Client (for future use)
        if (await _applicationManager.FindByClientIdAsync("customer-web") is null)
        {
            await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "customer-web",
                DisplayName = "R2.ShopNet Customer Web Application",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                }
            });

            _logger.LogInformation("Created client: customer-web");
        }

        // Mobile Application Client (for future use)
        if (await _applicationManager.FindByClientIdAsync("mobile-app") is null)
        {
            await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "mobile-app",
                DisplayName = "R2.ShopNet Mobile Application",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                }
            });

            _logger.LogInformation("Created client: mobile-app");
        }

        // Postman/Testing Client (for development testing)
        if (await _applicationManager.FindByClientIdAsync("postman-client") is null)
        {
            await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "postman-client",
                ClientSecret = "postman-secret-dev-only", // Only for dev!
                DisplayName = "Postman Testing Client",
                ClientType = ClientTypes.Confidential, // Confidential client with secret
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api",
                    Permissions.Prefixes.Scope + "admin"
                }
            });

            _logger.LogInformation("Created client: postman-client");
        }
    }
}
