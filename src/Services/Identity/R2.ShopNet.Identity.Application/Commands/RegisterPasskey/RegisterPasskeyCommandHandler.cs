using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Domain.Entities;
using System.Text.Json;

namespace R2.ShopNet.Identity.Application.Commands.RegisterPasskey;

/// <summary>
/// Handler for passkey registration command.
/// Initiates passkey registration by generating WebAuthn creation options.
/// </summary>
public class RegisterPasskeyCommandHandler : ICommandHandler<RegisterPasskeyCommand, Result<RegisterPasskeyResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterPasskeyCommandHandler> _logger;

    public RegisterPasskeyCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterPasskeyCommandHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<Result<RegisterPasskeyResponse>> Handle(RegisterPasskeyCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Find user
            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found for passkey registration: {UserId}", command.UserId);
                return Result.Failure<RegisterPasskeyResponse>(Error.NotFound("USER_NOT_FOUND", "User not found."));
            }

            if (!user.CanLogin())
            {
                _logger.LogWarning("User cannot login, passkey registration denied: {UserId}", command.UserId);
                return Result.Failure<RegisterPasskeyResponse>(Error.Validation("USER_INACTIVE", "User account is not active."));
            }

            // Generate passkey creation options
            var optionsJson = await _signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = user.Id.ToString(),
                Name = user.Email ?? user.UserName ?? user.Id.ToString(),
                DisplayName = user.FullName
            });

            if (string.IsNullOrEmpty(optionsJson))
            {
                _logger.LogError("Failed to generate passkey creation options for user: {UserId}", command.UserId);
                return Result.Failure<RegisterPasskeyResponse>(
                    Error.Failure("PASSKEY_OPTIONS_FAILED", "Failed to generate passkey registration options."));
            }

            // Parse to extract challenge
            var optionsDoc = JsonDocument.Parse(optionsJson);
            var challengeBase64 = optionsDoc.RootElement.GetProperty("challenge").GetString() ?? "";

            var response = new RegisterPasskeyResponse
            {
                RegistrationOptionsJson = optionsJson,
                Challenge = challengeBase64,
                Message = "Use your device's biometric authentication to register your passkey."
            };

            _logger.LogInformation("Passkey registration options generated for user: {UserId}", command.UserId);

            return Result<RegisterPasskeyResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during passkey registration for user: {UserId}", command.UserId);
            return Result.Failure<RegisterPasskeyResponse>(
                Error.Failure("PASSKEY_REGISTRATION_ERROR", "An error occurred during passkey registration."));
        }
    }
}
