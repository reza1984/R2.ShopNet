using Microsoft.AspNetCore.Identity;

namespace R2.ShopNet.Identity.Domain.Entities;

/// <summary>
/// Application role for ASP.NET Core Identity integration.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Id = Guid.NewGuid();
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}
