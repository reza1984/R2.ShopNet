using Microsoft.AspNetCore.Identity;

namespace R2.ShopNet.Identity.Domain.Entities;

/// <summary>
/// Represents a passkey credential for a user.
/// Stores WebAuthn credentials for passwordless authentication.
/// </summary>
public class ApplicationUserPasskey : IdentityUserPasskey<Guid>
{
    /// <summary>
    /// The date and time when this credential was last used for authentication.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// The user agent string from when this credential was created.
    /// Helps identify the device type (e.g., "MacBook", "iPhone").
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// The IP address from when this credential was created.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Indicates whether this credential is still active and can be used for authentication.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
