using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Queries.GetUserPasskeys;

/// <summary>
/// Query to get all passkeys for a specific user.
/// </summary>
public record GetUserPasskeysQuery : IQuery<List<PasskeyDto>>
{
    /// <summary>
    /// The ID of the user whose passkeys to retrieve.
    /// </summary>
    public required Guid UserId { get; init; }
}

/// <summary>
/// DTO representing a user's passkey.
/// </summary>
public record PasskeyDto
{
    /// <summary>
    /// The unique identifier of the passkey (base64url encoded credential ID).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The user ID this passkey belongs to.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The friendly name of the passkey (e.g., "MacBook Touch ID").
    /// </summary>
    public required string FriendlyName { get; init; }

    /// <summary>
    /// The credential ID (base64url encoded).
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// When the passkey was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the passkey was last used (null if never used).
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// Whether the passkey is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// User agent string from when the passkey was registered.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// IP address from when the passkey was registered.
    /// </summary>
    public string? IpAddress { get; init; }
}
