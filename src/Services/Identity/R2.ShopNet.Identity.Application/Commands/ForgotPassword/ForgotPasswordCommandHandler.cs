using System.Text;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Handler for forgot password command using ASP.NET Core Identity.
/// </summary>
[GenerateHandler]
public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return Result.Failure<ForgotPasswordResponse>(
                Error.Validation("Email.Invalid", "Invalid email format"));
        }

        // Find user by email
        var user = await _userManager.FindByEmailAsync(command.Email);

        // For security reasons, always return success even if user doesn't exist
        // This prevents email enumeration attacks
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", command.Email);

            // Return success message to prevent email enumeration
            return Result.Success(new ForgotPasswordResponse(
                "If an account with that email exists, a password reset link has been sent.",
                command.Email));
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset requested for inactive user: {UserId}", user.Id);

            // Return generic success message to prevent account enumeration
            return Result.Success(new ForgotPasswordResponse(
                "If an account with that email exists, a password reset link has been sent.",
                command.Email));
        }

        // Generate password reset token
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);

        // Send password reset email
        try
        {
            // Create the reset URL (points to the client app's reset password page)
            var resetUrl = "http://localhost:4200/reset-password"; // This will be configured from EmailSettings.ClientBaseUrl
            
            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                resetToken,
                resetUrl,
                cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            // Don't expose email sending failure to user for security
        }

        return Result.Success(new ForgotPasswordResponse(
            "If an account with that email exists, a password reset link has been sent.",
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
