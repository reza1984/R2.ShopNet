using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;

/// <summary>
/// Handler for passkey authentication.
/// Verifies the passkey assertion and issues an access token.
/// </summary>
public class LoginWithPasskeyCommandHandler : ICommandHandler<LoginWithPasskeyCommand, Result<LoginWithPasskeyResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginWithPasskeyCommandHandler> _logger;

    public LoginWithPasskeyCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<LoginWithPasskeyCommandHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<LoginWithPasskeyResponse>> Handle(
        LoginWithPasskeyCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Perform passkey sign-in
            // PasskeySignInAsync takes the assertion JSON string directly
            var signInResult = await _signInManager.PasskeySignInAsync(command.AssertionResponseJson);

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Passkey authentication failed");

                if (signInResult.IsLockedOut)
                {
                    return Result.Failure<LoginWithPasskeyResponse>(
                        Error.Validation("ACCOUNT_LOCKED", "Account is locked. Please try again later."));
                }

                if (signInResult.IsNotAllowed)
                {
                    return Result.Failure<LoginWithPasskeyResponse>(
                        Error.Validation("NOT_ALLOWED", "Sign-in not allowed. Please verify your account."));
                }

                return Result.Failure<LoginWithPasskeyResponse>(
                    Error.Validation("AUTHENTICATION_FAILED", "Passkey authentication failed."));
            }

            // Get the authenticated user from HttpContext after successful sign-in
            // SignInManager signs in the user and sets the authentication context
            var user = await _signInManager.UserManager.GetUserAsync(_signInManager.Context.User);

            if (user == null)
            {
                _logger.LogError("Could not retrieve user after successful passkey sign-in");
                return Result.Failure<LoginWithPasskeyResponse>(
                    Error.Failure("USER_NOT_FOUND", "User not found after authentication."));
            }

            // Update last login
            user.RecordSuccessfulLogin();
            await _userManager.UpdateAsync(user);

            // Generate JWT token
            var token = await _tokenService.GenerateAccessTokenAsync(user);

            _logger.LogInformation("User {UserId} authenticated successfully with passkey", user.Id);

            var response = new LoginWithPasskeyResponse
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                AccessToken = token,
                ExpiresIn = 3600,
                Message = $"Welcome back, {user.FirstName ?? user.Email}!"
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during passkey authentication");
            return Result.Failure<LoginWithPasskeyResponse>(
                Error.Failure("PASSKEY_LOGIN_ERROR", "An error occurred during passkey authentication."));
        }
    }
}
