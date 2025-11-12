using R2.ShopNet.Framework.Identity.Entities;

namespace R2.ShopNet.Identity.Domain.Entities;

/// <summary>
/// Identity service-specific user entity.
/// Inherits from framework ApplicationUser with GUIDv7 support.
/// </summary>
public class ApplicationUser : Framework.Identity.Entities.ApplicationUser
{
    public ApplicationUser() : base()
    {
    }

    public ApplicationUser(string email, string? firstName = null, string? lastName = null)
        : base(email, firstName, lastName)
    {
    }

    /// <summary>
    /// Enforces email confirmation for the Identity service.
    /// </summary>
    protected override bool RequireEmailConfirmation() => false; // Set to true in production

    /// <summary>
    /// Navigation property for passkey credentials
    /// </summary>
    public ICollection<PasskeyCredential> PasskeyCredentials { get; set; } = new List<PasskeyCredential>();
}
