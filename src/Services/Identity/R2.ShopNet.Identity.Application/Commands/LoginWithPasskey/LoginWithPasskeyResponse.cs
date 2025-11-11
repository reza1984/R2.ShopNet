namespace R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;

/// <summary>
/// Response after successful passkey authentication.
/// Contains user information for token generation by the controller.
/// </summary>
public record LoginWithPasskeyResponse
{
    /// <summary>
    /// The authenticated user's ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The authenticated user's email.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The authenticated user's username.
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// JWT access token for API authentication (set by controller).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens (set by controller).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// ID token containing user claims (set by controller).
    /// </summary>
    public string? IdToken { get; set; }

    /// <summary>
    /// Token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; } = 3600;
}
