using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R2.ShopNet.Identity.Application.Interfaces;
using R2.ShopNet.Identity.Application.DTOs.Passkey;
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
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        IPasskeyService passkeyService,
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ILogger<PasskeyController> logger)
    {
        _passkeyService = passkeyService;
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
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
    public async Task<ActionResult<PasskeyAuthenticationOptions>> BeginAuthentication([FromBody] BeginAuthenticationRequest? request = null)
    {
        var options = await _passkeyService.BeginAuthenticationAsync(request?.Username);
        return Ok(options);
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
