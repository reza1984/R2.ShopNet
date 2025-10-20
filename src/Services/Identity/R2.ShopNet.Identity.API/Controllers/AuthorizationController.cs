using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using R2.ShopNet.Identity.Domain.Entities;
using System.Collections.Immutable;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace R2.ShopNet.Identity.API.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordFlowAsync(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenFlowAsync(request);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    [HttpGet("~/connect/endsession")]
    [HttpPost("~/connect/endsession")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Sign out the user from the application
        await _signInManager.SignOutAsync();

        // Return a SignOutResult that will redirect to post_logout_redirect_uri
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = request.PostLogoutRedirectUri
            });
    }

    private async Task<IActionResult> HandlePasswordFlowAsync(OpenIddictRequest request)
    {
        // Find user by username (email)
        var user = await _userManager.FindByNameAsync(request.Username!) ??
                   await _userManager.FindByEmailAsync(request.Username!);

        if (user == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The username/password couple is invalid."
            });
        }

        // Validate the password
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = result.IsLockedOut
                    ? "The user account has been locked out."
                    : "The username/password couple is invalid."
            });
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The user account is not active."
            });
        }

        // Create the claims principal
        var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Return the token
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenFlowAsync(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the refresh token
        var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Retrieve the user profile corresponding to the refresh token
        var user = await _userManager.FindByIdAsync(info.Principal!.GetClaim(Claims.Subject)!);

        if (user == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The refresh token is no longer valid."
            });
        }

        // Ensure the user is still allowed to sign in
        if (!await _signInManager.CanSignInAsync(user) || !user.IsActive)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The user is no longer allowed to sign in."
            });
        }

        // Create a new claims principal
        var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

        // Return the token
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user, ImmutableArray<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // Add standard claims
        identity.SetClaim(Claims.Subject, user.Id.ToString())
                .SetClaim(Claims.Email, user.Email)
                .SetClaim(Claims.Name, user.FullName)
                .SetClaim(Claims.PreferredUsername, user.UserName)
                .SetClaim("user_id", user.Id.ToString())
                .SetClaim("email_verified", user.EmailConfirmed.ToString());

        // Add custom claims
        if (!string.IsNullOrEmpty(user.FirstName))
            identity.SetClaim("first_name", user.FirstName);

        if (!string.IsNullOrEmpty(user.LastName))
            identity.SetClaim("last_name", user.LastName);

        // Add role claims
        var roles = await _userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, roles.ToImmutableArray());

        // Set the list of scopes granted to the client application
        identity.SetScopes(scopes);

        // Set resource servers (audience)
        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }
        identity.SetResources(resources.ToImmutableArray());

        // Set destinations for claims (which claims go in access token vs identity token)
        identity.SetDestinations(GetDestinations);

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens
        // To allow OpenIddict to serialize them, you must attach them to a destination
        // The DestinationSelector determines which claims go in which token

        switch (claim.Type)
        {
            // Always include these claims in both access and identity tokens
            case Claims.Name:
            case Claims.Email:
            case Claims.PreferredUsername:
            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            // Only include roles in access token for authorization
            case Claims.Role:
                yield return Destinations.AccessToken;
                yield break;

            // Include custom claims only in access token
            case "user_id":
            case "first_name":
            case "last_name":
            case "email_verified":
                yield return Destinations.AccessToken;
                yield break;

            // For all other claims, include only if they have a destination
            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
