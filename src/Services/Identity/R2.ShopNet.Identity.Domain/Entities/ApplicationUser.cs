using Microsoft.AspNetCore.Identity;

namespace R2.ShopNet.Identity.Domain.Entities;

/// <summary>
/// Application user for ASP.NET Core Identity integration.
/// Inherits from IdentityUser to leverage Microsoft Identity Manager.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public ApplicationUser(string email, string? firstName = null, string? lastName = null) : this()
    {
        Email = email;
        UserName = email; // Use email as username
        NormalizedEmail = email.ToUpperInvariant();
        NormalizedUserName = email.ToUpperInvariant();
        FirstName = firstName;
        LastName = lastName;
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanLogin()
    {
        if (!IsActive)
            return false;

        if (LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow)
            return false;

        return true;
    }
}
