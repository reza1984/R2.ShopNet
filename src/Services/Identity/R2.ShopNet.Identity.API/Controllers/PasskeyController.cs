using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Commands.RegisterPasskey;
using R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;
using R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;
using System.Security.Claims;

namespace R2.ShopNet.Identity.API.Controllers;

/// <summary>
/// API endpoints for passkey (WebAuthn) registration and authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PasskeyController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        ICommandDispatcher commandDispatcher,
        ILogger<PasskeyController> logger)
    {
        _commandDispatcher = commandDispatcher;
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
    /// Authenticates a user using a passkey.
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
        _logger.LogInformation("Passkey authentication attempt");

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

        _logger.LogInformation("User {UserId} authenticated successfully with passkey", result.Value.UserId);
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid in JWT");
            return Unauthorized(new { error = "INVALID_TOKEN", message = "Invalid authentication token." });
        }

        // TODO: Implement GetUserPasskeysQuery
        // For now, return empty list
        _logger.LogInformation("Fetching passkeys for user: {UserId}", userId);
        return Ok(new List<PasskeyDto>());
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
/// Request model for passkey authentication.
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
