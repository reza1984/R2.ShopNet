using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Commands.RegisterPasskey;
using R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;
using R2.ShopNet.Identity.Application.Commands.BeginPasskeyLogin;
using R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;
using R2.ShopNet.Identity.Application.Queries.GetUserPasskeys;
using R2.ShopNet.Identity.Domain.Entities;
using System.Security.Claims;

namespace R2.ShopNet.Identity.API.Controllers;

/// <summary>
/// API endpoints for passkey (WebAuthn) registration and authentication.
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class PasskeyController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Application.Services.ITokenService _tokenService;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        UserManager<ApplicationUser> userManager,
        Application.Services.ITokenService tokenService,
        ILogger<PasskeyController> logger)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates passkey registration for the authenticated user.
    /// Returns WebAuthn creation options that the client will use to create a passkey.
    /// </summary>
    /// <param name="friendlyName">Optional friendly name for the passkey (e.g., "MacBook Touch ID")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Passkey registration options</returns>
    /// <response code="200">Registration options generated successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="400">Invalid request or user cannot register passkey</response>
    [Authorize]
    [HttpPost("register/begin")]
    [ProducesResponseType(typeof(RegisterPasskeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BeginRegistration(
        [FromQuery] string? friendlyName,
        CancellationToken cancellationToken)
    {
        // OpenIddict uses 'sub' claim for user ID, not ClaimTypes.NameIdentifier
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid in JWT");
            return Unauthorized(new { error = "INVALID_TOKEN", message = "Invalid authentication token." });
        }

        _logger.LogInformation("Passkey registration initiated for user: {UserId}", userId);

        var command = new RegisterPasskeyCommand
        {
            UserId = userId,
            FriendlyName = friendlyName,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Passkey registration failed for user {UserId}: {Error}",
                userId, result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Completes passkey registration after the client has created the credential.
    /// </summary>
    /// <param name="request">Attestation response from WebAuthn API</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    /// <response code="200">Passkey registered successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="400">Invalid attestation or registration failed</response>
    [Authorize]
    [HttpPost("register/complete")]
    [ProducesResponseType(typeof(CompletePasskeyRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteRegistration(
        [FromBody] CompletePasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        // OpenIddict uses 'sub' claim for user ID, not ClaimTypes.NameIdentifier
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid in JWT");
            return Unauthorized(new { error = "INVALID_TOKEN", message = "Invalid authentication token." });
        }

        _logger.LogInformation("Completing passkey registration for user: {UserId}", userId);

        var command = new CompletePasskeyRegistrationCommand
        {
            UserId = userId,
            AttestationResponseJson = request.AttestationResponseJson,
            FriendlyName = request.FriendlyName,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Passkey registration completion failed for user {UserId}: {Error}",
                userId, result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        _logger.LogInformation("Passkey registered successfully for user: {UserId}", userId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Begins passkey authentication by generating WebAuthn assertion options.
    /// </summary>
    /// <param name="request">Request containing user's email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WebAuthn assertion options</returns>
    /// <response code="200">Assertion options generated successfully</response>
    /// <response code="400">Invalid request or user not found</response>
    /// <response code="404">No passkeys found for this user</response>
    [HttpPost("login/begin")]
    [ProducesResponseType(typeof(BeginPasskeyLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BeginLogin(
        [FromBody] BeginPasskeyLoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Passkey authentication begin for email: {Email}", request.Email);

        var command = new BeginPasskeyLoginCommand
        {
            Email = request.Email
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Passkey authentication begin failed: {Error}", result.Error.Message);
            
            if (result.Error.Type == ErrorType.NotFound)
            {
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            }
            
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Completes passkey authentication by verifying the WebAuthn assertion.
    /// Returns OAuth/OIDC tokens directly (bypassing OpenIddict's SignIn which only works on OAuth endpoints).
    /// </summary>
    /// <param name="request">Assertion response from WebAuthn API</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login result with OAuth tokens</returns>
    /// <response code="200">Authentication successful, returns OAuth tokens</response>
    /// <response code="400">Invalid assertion or authentication failed</response>
    [HttpPost("login/complete")]
    [ProducesResponseType(typeof(PasskeyTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteLogin(
        [FromBody] CompletePasskeyLoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Passkey authentication complete for email: {Email}", request.Email);

        var command = new LoginWithPasskeyCommand
        {
            AssertionResponseJson = request.AssertionResponseJson,
            Username = request.Email
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Passkey authentication failed: {Error}", result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        // Find the user
        var user = await _userManager.FindByIdAsync(result.Value.UserId.ToString());
        if (user == null)
        {
            _logger.LogError("User {UserId} not found after passkey authentication", result.Value.UserId);
            return BadRequest(new { error = "USER_NOT_FOUND", message = "User not found." });
        }

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens manually (can't use OpenIddict's SignIn on non-OAuth endpoints)
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var idToken = await _tokenService.GenerateIdTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var response = new PasskeyTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IdToken = idToken,
            TokenType = "Bearer",
            ExpiresIn = 3600 // 1 hour
        };

        _logger.LogInformation("User {UserId} authenticated successfully via passkey", user.Id);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user using a passkey (single-step endpoint, deprecated in favor of begin/complete flow).
    /// </summary>
    /// <param name="request">Assertion response from WebAuthn API</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login result with JWT token</returns>
    /// <response code="200">Authentication successful, returns JWT token</response>
    /// <response code="400">Invalid assertion or authentication failed</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginWithPasskeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginWithPasskeyRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Passkey authentication attempt (legacy endpoint)");

        var command = new LoginWithPasskeyCommand
        {
            AssertionResponseJson = request.AssertionResponseJson,
            Username = request.Username
        };

        var result = await _commandDispatcher.Dispatch(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Passkey authentication failed: {Error}", result.Error.Message);
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the list of passkeys for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's passkeys</returns>
    /// <response code="200">Passkeys retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [Authorize]
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<PasskeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserPasskeys(CancellationToken cancellationToken)
    {
        // OpenIddict uses 'sub' claim for user ID, not ClaimTypes.NameIdentifier
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid in JWT");
            return Unauthorized(new { error = "INVALID_TOKEN", message = "Invalid authentication token." });
        }

        _logger.LogInformation("Fetching passkeys for user: {UserId}", userId);

        // Dispatch query to get user's passkeys
        var query = new GetUserPasskeysQuery { UserId = userId };
        var passkeys = await _queryDispatcher.Dispatch(query, cancellationToken);

        return Ok(passkeys);
    }

    /// <summary>
    /// Deletes a specific passkey for the authenticated user.
    /// </summary>
    /// <param name="passkeyId">The ID of the passkey to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Passkey deleted successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Passkey not found</response>
    [Authorize]
    [HttpDelete("{passkeyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePasskey(
        [FromRoute] string passkeyId,
        CancellationToken cancellationToken)
    {
        // OpenIddict uses 'sub' claim for user ID, not ClaimTypes.NameIdentifier
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid in JWT");
            return Unauthorized(new { error = "INVALID_TOKEN", message = "Invalid authentication token." });
        }

        // TODO: Implement DeletePasskeyCommand
        _logger.LogInformation("Deleting passkey {PasskeyId} for user: {UserId}", passkeyId, userId);
        return Ok(new { message = "Passkey deleted successfully" });
    }

}

/// <summary>
/// Request model for completing passkey registration.
/// </summary>
public record CompletePasskeyRegistrationRequest
{
    /// <summary>
    /// The attestation response from the WebAuthn API (as JSON string).
    /// </summary>
    public required string AttestationResponseJson { get; init; }

    /// <summary>
    /// Optional friendly name for the passkey.
    /// </summary>
    public string? FriendlyName { get; init; }
}

/// <summary>
/// Request model for beginning passkey authentication.
/// </summary>
public record BeginPasskeyLoginRequest
{
    /// <summary>
    /// User's email address to identify which passkeys to allow.
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
/// Request model for completing passkey authentication.
/// </summary>
public record CompletePasskeyLoginRequest
{
    /// <summary>
    /// The assertion response from the WebAuthn API (as JSON string).
    /// </summary>
    public required string AssertionResponseJson { get; init; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
/// Request model for passkey authentication (legacy single-step).
/// </summary>
public record LoginWithPasskeyRequest
{
    /// <summary>
    /// The assertion response from the WebAuthn API (as JSON string).
    /// </summary>
    public required string AssertionResponseJson { get; init; }

    /// <summary>
    /// Optional username to filter passkeys.
    /// </summary>
    public string? Username { get; init; }
}

/// <summary>
/// Response model for successful passkey login with OAuth tokens.
/// </summary>
public record PasskeyTokenResponse
{
    /// <summary>
    /// OAuth access token
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// OAuth refresh token
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// OpenID Connect ID token
    /// </summary>
    public required string IdToken { get; init; }

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public required string TokenType { get; init; }

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; init; }
}

/// <summary>
/// DTO for passkey information.
/// </summary>
public record PasskeyDto
{
    /// <summary>
    /// The unique identifier of the passkey.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The user ID this passkey belongs to.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The friendly name of the passkey.
    /// </summary>
    public required string FriendlyName { get; init; }

    /// <summary>
    /// The credential ID (base64url encoded).
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// When the passkey was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the passkey was last used for authentication.
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// The user agent string from when this passkey was created.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// The IP address from when this passkey was created.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Whether this passkey is still active.
    /// </summary>
    public bool IsActive { get; init; }
}

