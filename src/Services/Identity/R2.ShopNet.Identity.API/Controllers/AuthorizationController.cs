using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using R2.ShopNet.Identity.Domain.Entities;
using System.Collections.Immutable;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using R2.ShopNet.Identity.Application.Interfaces;
using R2.ShopNet.Identity.Application.DTOs.Passkey;

namespace R2.ShopNet.Identity.API.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IPasskeyService _passkeyService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        IPasskeyService passkeyService,
        ILogger<AuthorizationController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _passkeyService = passkeyService;
        _logger = logger;
    }

    /// <summary>
    /// Handles authorization requests (Authorization Code Flow)
    /// </summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        Console.WriteLine("=== Authorization Request Started ===");
        Console.WriteLine($"Request Method: {Request.Method}");
        Console.WriteLine($"Request Path: {Request.Path}");
        Console.WriteLine($"Request Query: {Request.QueryString}");

        _logger.LogInformation("=== Authorization Request Started ===");
        _logger.LogInformation("Request Method: {Method}", Request.Method);
        _logger.LogInformation("Request Path: {Path}", Request.Path);
        _logger.LogInformation("Request Query: {Query}", Request.QueryString);

        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        _logger.LogInformation("OpenIddict Request Details:");
        _logger.LogInformation("  ClientId: {ClientId}", request.ClientId);
        _logger.LogInformation("  RedirectUri: {RedirectUri}", request.RedirectUri);
        _logger.LogInformation("  ResponseType: {ResponseType}", request.ResponseType);
        _logger.LogInformation("  Scopes: {Scopes}", string.Join(", ", request.GetScopes()));

        // Retrieve the user principal stored in the authentication cookie
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("Authentication Cookie Check: {Succeeded}", result.Succeeded);

        // If the user is not authenticated, redirect to login
        if (!result.Succeeded)
        {
            var redirectUri = Request.PathBase + Request.Path + QueryString.Create(
                Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());

            Console.WriteLine($"User not authenticated. Challenging with redirect to: {redirectUri}");
            _logger.LogWarning("User not authenticated. Challenging with redirect to: {RedirectUri}", redirectUri);

            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = redirectUri
                });
        }

        // Get the user from the authentication cookie
        var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID cannot be found in the authentication cookie.");

        _logger.LogInformation("User authenticated. UserId: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogError("User {UserId} not found in database", userId);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The user associated with the authentication cookie no longer exists."
                }));
        }

        _logger.LogInformation("User found: {Email}", user.Email);

        // Retrieve the application details from the database
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application cannot be found.");

        _logger.LogInformation("Application found: {ClientId}", request.ClientId);

        // Retrieve the permanent authorizations associated with the user and the calling application
        var authorizations = await _authorizationManager.FindAsync(
            subject: userId,
            client: (await _applicationManager.GetIdAsync(application))!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        // Check if consent is required (optional - you can skip this for trusted apps)
        var consentType = await _applicationManager.GetConsentTypeAsync(application);

        switch (consentType)
        {
            case ConsentTypes.External when authorizations.Count == 0:
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The user must grant access to this application."
                    }));

            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Count > 0:
            case ConsentTypes.Explicit when authorizations.Count > 0 && !request.HasPrompt(Prompts.Consent):
                break;

            case ConsentTypes.Explicit when request.HasPrompt(Prompts.Consent):
            case ConsentTypes.Systematic:
                // Auto-approve for trusted first-party apps
                break;

            default:
                throw new InvalidOperationException("Invalid consent type.");
        }

        // Create authorization if it doesn't exist
        var authorization = authorizations.LastOrDefault();
        if (authorization == null)
        {
            authorization = await _authorizationManager.CreateAsync(
                identity: result.Principal!.Identity as ClaimsIdentity ?? throw new InvalidOperationException("Invalid identity"),
                subject: userId,
                client: (await _applicationManager.GetIdAsync(application))!,
                type: AuthorizationTypes.Permanent,
                scopes: request.GetScopes());
        }

        // Create claims principal for the token
        var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

        // Set the authorization ID
        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

        // Ask OpenIddict to generate the authorization response
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles userinfo requests
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        Console.WriteLine("=== UserInfo Request ===");
        Console.WriteLine($"User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
        Console.WriteLine($"User.Identity.AuthenticationType: {User.Identity?.AuthenticationType}");
        Console.WriteLine($"Authorization Header: {Request.Headers["Authorization"]}");
        Console.WriteLine($"Claims Count: {User.Claims.Count()}");

        _logger.LogInformation("=== UserInfo Request ===");
        _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
        _logger.LogInformation("Authorization Header: {AuthHeader}", Request.Headers["Authorization"]);

        // Try to get userId from multiple possible claim types
        var userId = User.FindFirst(Claims.Subject)?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        Console.WriteLine($"UserId from claims: {userId}");
        Console.WriteLine($"All claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        _logger.LogInformation("UserId from claims: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("UserInfo: UserId is null or empty - returning Challenge");
            _logger.LogWarning("UserInfo: UserId is null or empty");
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The specified access token is invalid."
                }));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The user associated with this token no longer exists."
                }));
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = userId
        };

        // Add claims based on granted scopes
        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = user.Email!;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        if (User.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = user.FullName ?? user.UserName!;
            claims[Claims.PreferredUsername] = user.UserName!;
            if (!string.IsNullOrEmpty(user.FirstName))
                claims[Claims.GivenName] = user.FirstName;
            if (!string.IsNullOrEmpty(user.LastName))
                claims[Claims.FamilyName] = user.LastName;
        }

        if (User.HasScope(Scopes.Roles))
        {
            var roles = await _userManager.GetRolesAsync(user);
            claims[Claims.Role] = roles;
        }

        return Ok(claims);
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType())
        {
            return await HandleAuthorizationCodeFlowAsync(request);
        }

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordFlowAsync(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenFlowAsync(request);
        }

        // Passkey grant type
        if (request.GrantType == "urn:ietf:params:oauth:grant-type:passkey")
        {
            return await HandlePasskeyFlowAsync(request);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });

    }

    private async Task<IActionResult> HandleAuthorizationCodeFlowAsync(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the authorization code
        var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Retrieve the user profile corresponding to the authorization code
        var userId = info.Principal!.GetClaim(Claims.Subject);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The authorization code is no longer valid."
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

        // Restore the authorization ID from the authentication cookie
        principal.SetAuthorizationId(info.Principal.GetAuthorizationId());

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Return the token
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePasskeyFlowAsync(OpenIddictRequest request)
    {
        // Extract passkey assertion from request
        var assertionJson = request.Assertion;

        if (string.IsNullOrEmpty(assertionJson))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidRequest,
                ErrorDescription = "The passkey assertion is missing."
            });
        }

        // Deserialize the WebAuthn assertion response
        PasskeyAuthenticationResponse? assertion;
        try
        {
            assertion = System.Text.Json.JsonSerializer.Deserialize<PasskeyAuthenticationResponse>(assertionJson);
            if (assertion == null)
            {
                throw new InvalidOperationException("Failed to deserialize assertion.");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidRequest,
                ErrorDescription = $"Invalid assertion format: {ex.Message}"
            });
        }

        // Verify the passkey assertion using PasskeyService
        var result = await _passkeyService.CompleteAuthenticationAsync(assertion);

        if (!result.Success || result.UserId == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = result.ErrorMessage ?? "Passkey authentication failed."
            });
        }

        // Find the user
        var user = await _userManager.FindByIdAsync(result.UserId.Value.ToString());

        if (user == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The user account no longer exists."
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

        // Check if user can sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The user is not allowed to sign in."
            });
        }


        // Ensure 'openid' scope is present for id_token issuance
        var scopes = request.GetScopes();


        // Create the SAME claims principal as password flow
        var principal = await CreateClaimsPrincipalAsync(user, scopes);

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Return the token
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/endsession")]
    [HttpPost("~/connect/endsession")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Sign out from ASP.NET Core Identity (ApplicationCookie)
        await _signInManager.SignOutAsync();

        // Also sign out from the Cookie authentication scheme used for authorization flow
        // This is critical - without this, the R2.ShopNet.Identity cookie remains valid
        // and the user gets automatically re-authenticated on the next authorize request
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

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
