using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Domain.Entities;
using System.Text.Json;

namespace R2.ShopNet.Identity.Application.Commands.BeginPasskeyLogin;

/// <summary>
/// Handler for beginning passkey authentication.
/// Generates WebAuthn assertion options for the specified user.
/// </summary>
public class BeginPasskeyLoginCommandHandler : ICommandHandler<BeginPasskeyLoginCommand, Result<BeginPasskeyLoginResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<BeginPasskeyLoginCommandHandler> _logger;

    public BeginPasskeyLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<BeginPasskeyLoginCommandHandler> _logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        this._logger = _logger;
    }

    public async Task<Result<BeginPasskeyLoginResponse>> Handle(
        BeginPasskeyLoginCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user == null)
            {
                _logger.LogWarning("User not found for passkey login: {Email}", command.Email);
                return Result.Failure<BeginPasskeyLoginResponse>(
                    Error.NotFound("USER_NOT_FOUND", "No account found with this email address."));
            }

            if (!user.CanLogin())
            {
                _logger.LogWarning("User cannot login, passkey authentication denied: {Email}", command.Email);
                return Result.Failure<BeginPasskeyLoginResponse>(
                    Error.Validation("USER_INACTIVE", "User account is not active."));
            }

            // Get user's passkeys from UserManager
            var passkeys = await _userManager.GetPasskeysAsync(user);
            if (passkeys == null || !passkeys.Any())
            {
                _logger.LogWarning("No passkeys found for user: {Email}", command.Email);
                return Result.Failure<BeginPasskeyLoginResponse>(
                    Error.NotFound("NO_PASSKEYS", "No passkeys registered for this account."));
            }

            // Use SignInManager to create passkey request options
            // This establishes the session context needed for PasskeySignInAsync
            var passkeyRequestOptionsJson = await _signInManager.MakePasskeyRequestOptionsAsync(user);

            // Extract the challenge from the options for the response
            var optionsDocument = JsonDocument.Parse(passkeyRequestOptionsJson);
            var challenge = optionsDocument.RootElement.GetProperty("challenge").GetString() ?? "";

            var response = new BeginPasskeyLoginResponse
            {
                AssertionOptionsJson = passkeyRequestOptionsJson,
                Challenge = challenge
            };

            _logger.LogInformation("Passkey assertion options generated for user: {Email} with {Count} passkeys", 
                command.Email, passkeys.Count());

            return Result<BeginPasskeyLoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during passkey authentication begin for user: {Email}", command.Email);
            return Result.Failure<BeginPasskeyLoginResponse>(
                Error.Failure("PASSKEY_LOGIN_BEGIN_ERROR", "An error occurred while starting passkey authentication."));
        }
    }
}
