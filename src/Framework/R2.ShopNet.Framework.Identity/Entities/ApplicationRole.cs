using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.Identity.Entities;

/// <summary>
/// Base application role for ASP.NET Core Identity integration.
/// Uses GUID Version 7 for better database performance.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ApplicationRole() : base()
    {
        // Use GUID Version 7 for better database indexing
        Id = Guid.CreateVersion7();
        CreatedAt = DateTime.UtcNow;
    }

    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        // Use GUID Version 7 for better database indexing
        Id = Guid.CreateVersion7();
        Name = roleName;
        NormalizedName = roleName.ToUpperInvariant();
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the role description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
