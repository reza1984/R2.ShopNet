using System.Text;
using System.Web;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Infrastructure.Configuration;

namespace R2.ShopNet.Identity.Infrastructure.Services;

/// <summary>
/// Email service implementation using MailKit.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // For development with MailDev, we don't need SSL
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, _emailSettings.EnableSsl, cancellationToken);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
            {
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        var encodedToken = HttpUtility.UrlEncode(resetToken);
        var encodedEmail = HttpUtility.UrlEncode(email);
        var resetLink = $"{resetUrl}?token={encodedToken}&email={encodedEmail}";

        var subject = "Reset Your Password - ShopNet";
        var body = GetPasswordResetEmailTemplate(email, resetLink);

        await SendEmailAsync(email, subject, body, isHtml: true, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(
        string email,
        string verificationToken,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        var encodedToken = HttpUtility.UrlEncode(verificationToken);
        var encodedEmail = HttpUtility.UrlEncode(email);
        var verificationLink = $"{verificationUrl}?token={encodedToken}&email={encodedEmail}";

        var subject = "Verify Your Email - ShopNet";
        var body = GetEmailVerificationTemplate(email, verificationLink);

        await SendEmailAsync(email, subject, body, isHtml: true, cancellationToken);
    }

    private static string GetPasswordResetEmailTemplate(string email, string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Your Password</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #f8f9fa; padding: 30px; border-radius: 10px;"">
        <h1 style=""color: #2c3e50; margin-bottom: 20px;"">Reset Your Password</h1>
        
        <p>Hello,</p>
        
        <p>We received a request to reset the password for your ShopNet account associated with <strong>{email}</strong>.</p>
        
        <p>Click the button below to reset your password:</p>
        
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{resetLink}"" 
               style=""background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;"">
                Reset Password
            </a>
        </div>
        
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; background-color: #fff; padding: 10px; border-radius: 5px; font-size: 12px;"">{resetLink}</p>
        
        <p><strong>This link will expire in 1 hour.</strong></p>
        
        <p>If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>
        
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">
        
        <p style=""color: #7f8c8d; font-size: 12px;"">
            This is an automated message from ShopNet. Please do not reply to this email.
        </p>
    </div>
</body>
</html>";
    }

    private static string GetEmailVerificationTemplate(string email, string verificationLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #f8f9fa; padding: 30px; border-radius: 10px;"">
        <h1 style=""color: #2c3e50; margin-bottom: 20px;"">Welcome to ShopNet!</h1>
        
        <p>Hello,</p>
        
        <p>Thank you for creating an account with ShopNet. To complete your registration, please verify your email address.</p>
        
        <p>Click the button below to verify your email:</p>
        
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{verificationLink}"" 
               style=""background-color: #27ae60; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;"">
                Verify Email
            </a>
        </div>
        
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; background-color: #fff; padding: 10px; border-radius: 5px; font-size: 12px;"">{verificationLink}</p>
        
        <p>If you didn't create this account, you can safely ignore this email.</p>
        
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">
        
        <p style=""color: #7f8c8d; font-size: 12px;"">
            This is an automated message from ShopNet. Please do not reply to this email.
        </p>
    </div>
</body>
</html>";
    }
}
