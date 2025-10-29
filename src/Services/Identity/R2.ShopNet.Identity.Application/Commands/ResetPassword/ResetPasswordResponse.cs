namespace R2.ShopNet.Identity.Application.Commands.ResetPassword;

/// <summary>
/// Response for reset password request.
/// </summary>
public record ResetPasswordResponse(
    string Message,
    string Email);
