namespace R2.ShopNet.Identity.Application.Services;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML supported)</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email with token.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="resetUrl">Full URL for password reset page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPasswordResetEmailAsync(
        string email,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email verification email.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="verificationToken">Email verification token</param>
    /// <param name="verificationUrl">Full URL for email verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailVerificationAsync(
        string email,
        string verificationToken,
        string verificationUrl,
        CancellationToken cancellationToken = default);
}
