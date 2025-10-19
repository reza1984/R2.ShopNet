namespace R2.ShopNet.Identity.Application.Commands.LoginUser;

/// <summary>
/// Response for successful user login.
/// </summary>
public record LoginUserResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
