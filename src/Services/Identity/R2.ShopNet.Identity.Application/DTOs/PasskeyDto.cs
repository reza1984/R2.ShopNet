namespace R2.ShopNet.Identity.Application.DTOs;

/// <summary>
/// DTO for passkey information.
/// </summary>
public record PasskeyDto
{
    /// <summary>
    /// The unique identifier of the passkey.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The user ID this passkey belongs to.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The friendly name of the passkey.
    /// </summary>
    public required string FriendlyName { get; init; }

    /// <summary>
    /// The credential ID (base64url encoded).
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// When the passkey was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the passkey was last used for authentication.
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// The user agent string from when this passkey was created.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// The IP address from when this passkey was created.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Whether this passkey is still active.
    /// </summary>
    public bool IsActive { get; init; }
}
