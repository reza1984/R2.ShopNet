using R2.ShopNet.Framework.Identity.Entities;

namespace R2.ShopNet.Identity.Domain.Entities;

/// <summary>
/// Identity service-specific role entity.
/// Inherits from framework ApplicationRole with GUIDv7 support.
/// </summary>
public class ApplicationRole : Framework.Identity.Entities.ApplicationRole
{
    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName, string? description = null)
        : base(roleName, description)
    {
    }
}
