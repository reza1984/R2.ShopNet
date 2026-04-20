namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Response for forgot password request.
/// </summary>
public record ForgotPasswordResponse(
    string Message,
    string Email);
