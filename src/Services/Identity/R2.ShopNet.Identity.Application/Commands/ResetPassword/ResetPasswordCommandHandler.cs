using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Handler for reset password command using ASP.NET Core Identity.
/// </summary>
[GenerateHandler]
public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<ResetPasswordResponse>> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return Result.Failure<ResetPasswordResponse>(
                Error.Validation("Email.Invalid", "Invalid email format"));
        }

        // Validate token
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure<ResetPasswordResponse>(
                Error.Validation("Token.Invalid", "Password reset token is required"));
        }

        // Validate new password
        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < 8)
        {
            return Result.Failure<ResetPasswordResponse>(
                Error.Validation("Password.TooShort", "Password must be at least 8 characters"));
        }

        // Validate password confirmation
        if (command.NewPassword != command.ConfirmPassword)
        {
            return Result.Failure<ResetPasswordResponse>(
                Error.Validation("Password.Mismatch", "Password and confirmation password do not match"));
        }

        // Find user by email
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempted for non-existent email: {Email}", command.Email);

            // Return generic error to prevent email enumeration
            return Result.Failure<ResetPasswordResponse>(
                Error.NotFound("User.NotFound", "Invalid password reset request"));
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset attempted for inactive user: {UserId}", user.Id);

            return Result.Failure<ResetPasswordResponse>(
                Error.Validation("User.Inactive", "This account is inactive"));
        }

        // Reset password using token
        var result = await _userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Password reset failed for user {UserId}: {Errors}", user.Id, errors);

            return Result.Failure<ResetPasswordResponse>(
                Error.Validation("Password.ResetFailed", $"Failed to reset password: {errors}"));
        }

        // Update security stamp to invalidate existing tokens
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);

        return Result.Success(new ResetPasswordResponse(
            "Your password has been reset successfully. You can now sign in with your new password.",
            command.Email));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
