using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;

/// <summary>
/// Handler for completing passkey registration.
/// Verifies the attestation response and saves the passkey to the database.
/// </summary>
public class CompletePasskeyRegistrationCommandHandler : ICommandHandler<CompletePasskeyRegistrationCommand, Result<CompletePasskeyRegistrationResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<CompletePasskeyRegistrationCommandHandler> _logger;

    public CompletePasskeyRegistrationCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<CompletePasskeyRegistrationCommandHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<Result<CompletePasskeyRegistrationResponse>> Handle(
        CompletePasskeyRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find user
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found for passkey registration completion: {UserId}", command.UserId);
                return Result.Failure<CompletePasskeyRegistrationResponse>(
                    Error.NotFound("USER_NOT_FOUND", "User not found."));
            }

            // Perform passkey attestation (validates the response from client)
            var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(command.AttestationResponseJson);
            if (!attestationResult.Succeeded)
            {
                _logger.LogWarning("Passkey attestation failed for user {UserId}: {Message}",
                    command.UserId, attestationResult.Failure?.Message ?? "Unknown error");
                return Result.Failure<CompletePasskeyRegistrationResponse>(
                    Error.Validation("ATTESTATION_FAILED", attestationResult.Failure?.Message ?? "Passkey attestation failed."));
            }

            // Get the passkey from the attestation result
            var passkey = attestationResult.Passkey;
            if (passkey == null)
            {
                _logger.LogError("Passkey is null after successful attestation for user: {UserId}", command.UserId);
                return Result.Failure<CompletePasskeyRegistrationResponse>(
                    Error.Failure("PASSKEY_DATA_NULL", "Failed to retrieve passkey data."));
            }

            // Set friendly name if provided
            if (!string.IsNullOrWhiteSpace(command.FriendlyName))
            {
                passkey.Name = command.FriendlyName;
            }

            // Save the passkey to the database
            var addResult = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to save passkey for user {UserId}: {Errors}", command.UserId, errors);
                return Result.Failure<CompletePasskeyRegistrationResponse>(
                    Error.Failure("PASSKEY_SAVE_FAILED", $"Failed to save passkey: {errors}"));
            }

            _logger.LogInformation("Passkey registered successfully for user: {UserId}", command.UserId);

            var response = new CompletePasskeyRegistrationResponse
            {
                CredentialId = Convert.ToBase64String(passkey.CredentialId),
                FriendlyName = passkey.Name,
                Message = "Passkey registered successfully. You can now use it to sign in."
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration for user: {UserId}", command.UserId);
            return Result.Failure<CompletePasskeyRegistrationResponse>(
                Error.Failure("PASSKEY_REGISTRATION_ERROR", "An error occurred while completing passkey registration."));
        }
    }
}
