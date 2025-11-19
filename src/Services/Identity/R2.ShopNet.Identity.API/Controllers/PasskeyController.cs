using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Identity.Application.Interfaces;
using R2.ShopNet.Identity.Application.DTOs.Passkey;
using R2.ShopNet.Identity.Domain.Entities;
using System.Security.Claims;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Commands.Passkey.RegisterPasskey;
using R2.ShopNet.Identity.Application.Queries.Passkey.GetUserPasskeys;
using R2.ShopNet.Identity.Application.Commands.Passkey.DeletePasskey;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace R2.ShopNet.Identity.API.Controllers;

[ApiController]
[Route("passkey")] // Route updated to match expected endpoint for passkey authentication
public class PasskeyController : ControllerBase
{
    private readonly IPasskeyService _passkeyService;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        IPasskeyService passkeyService,
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        UserManager<ApplicationUser> userManager,
        ILogger<PasskeyController> logger)
    {
        _passkeyService = passkeyService;
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("register/begin")]
    [Authorize]
    [ProducesResponseType(typeof(PasskeyRegistrationOptions), 200)]
    public async Task<ActionResult<PasskeyRegistrationOptions>> BeginRegistration()
    {
        var userId = GetCurrentUserId();
        var options = await _passkeyService.BeginRegistrationAsync(userId);
        // Return registrationOptionsJson as a JSON string for frontend compatibility
        // Patch: WebAuthn expects rp as an object { name, id }
        var optionsObj = new {
            challenge = options.Challenge,
            rp = new { name = options.RpName, id = options.RpId },
            user = new {
                id = options.UserId,
                name = options.UserName,
                displayName = options.UserDisplayName
            },
            pubKeyCredParams = options.PubKeyCredParams,
            timeout = options.Timeout,
            attestation = options.Attestation,
            excludeCredentials = options.ExcludeCredentials,
            authenticatorSelection = options.AuthenticatorSelection
        };
        var json = System.Text.Json.JsonSerializer.Serialize(
            optionsObj,
            new System.Text.Json.JsonSerializerOptions {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }
        );
        return Ok(new {
            registrationOptionsJson = json
        });
    }

    [HttpPost("register/complete")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> CompleteRegistration([FromBody] PasskeyRegistrationRequest request)
    {
        var userId = GetCurrentUserId();
        var command = new RegisterPasskeyCommand(userId, request.Response, request.DeviceName);
        var result = await _commandDispatcher.Dispatch(command);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.Error });

        return Ok(new { CredentialId = result.Value, Message = "Passkey registered successfully" });
    }

    [HttpPost("authenticate/begin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasskeyAuthenticationOptions), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<PasskeyAuthenticationOptions>> BeginAuthentication([FromBody] BeginAuthenticationRequest? request = null)
    {
        try
        {
            var options = await _passkeyService.BeginAuthenticationAsync(request?.Username);
            return Ok(options);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("BeginAuthentication failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("BeginAuthentication failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Completes passkey authentication and sets authentication cookie for Blazor portal login
    /// </summary>
    [HttpPost("authenticate/complete")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> CompleteAuthentication([FromBody] CompleteAuthenticationRequest request)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        if (request.Assertion == null)
        {
            return BadRequest(new { message = "Passkey assertion is required" });
        }

        // Verify the passkey assertion
        var result = await _passkeyService.CompleteAuthenticationAsync(request.Assertion);

        if (!result.Success || result.UserId == null)
        {
            _logger.LogWarning("Passkey authentication failed for user {Username}: {Error}",
                request.Username, result.ErrorMessage);
            return BadRequest(new { message = result.ErrorMessage ?? "Passkey authentication failed" });
        }

        // Find the user
        var user = await _userManager.FindByIdAsync(result.UserId.Value.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found after successful passkey authentication: {UserId}", result.UserId);
            return BadRequest(new { message = "User account not found" });
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user attempted login: {UserId}", user.Id);
            return BadRequest(new { message = "User account is not active" });
        }

        // Create claims for the authentication cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        // Sign in the user with cookie authentication
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            authProperties);

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} successfully authenticated with passkey", user.Email);

        return Ok(new
        {
            success = true,
            message = "Authentication successful",
            userId = user.Id
        });
    }

    [HttpGet("credentials")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PasskeyCredentialDto>), 200)]
    public async Task<ActionResult> GetCredentials()
    {
        var userId = GetCurrentUserId();
        var query = new GetUserPasskeysQuery(userId);
        var result = await _queryDispatcher.Dispatch(query);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("credentials/{id}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> DeleteCredential(Guid id)
    {
        var userId = GetCurrentUserId();
        var command = new DeletePasskeyCommand(userId, id);
        var result = await _commandDispatcher.Dispatch(command);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.Error });

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(Claims.Subject)?.Value
                       ?? User.FindFirst("user_id")?.Value
                       ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(userIdClaim);
    }
}

public record PasskeyRegistrationRequest(
    PasskeyRegistrationResponse Response,
    string? DeviceName = null
);

public record BeginAuthenticationRequest(
    string? Username = null
);

public record CompleteAuthenticationRequest(
    string Username,
    PasskeyAuthenticationResponse Assertion
);
