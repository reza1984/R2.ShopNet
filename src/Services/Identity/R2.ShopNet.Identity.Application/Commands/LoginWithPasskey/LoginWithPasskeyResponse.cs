namespace R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;

/// <summary>
/// Response after successful passkey authentication.
/// </summary>
public record LoginWithPasskeyResponse
{
    /// <summary>
    /// The authenticated user's ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The user's full name.
    /// </summary>
    public string? FullName { get; init; }

    /// <summary>
    /// JWT access token for API authentication.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; } = 3600;

    /// <summary>
    /// Success message.
    /// </summary>
    public string Message { get; init; } = "Login successful.";
}
