using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.Identity.Entities;

/// <summary>
/// Base application user for ASP.NET Core Identity integration.
/// Inherits from IdentityUser to leverage Microsoft Identity Manager.
/// Uses GUID Version 7 for better database performance.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ApplicationUser()
    {
        // Use GUID Version 7 for better database indexing
        Id = Guid.CreateVersion7();
        CreatedAt = DateTime.UtcNow;
        SecurityStamp = Guid.CreateVersion7().ToString();
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

    /// <summary>
    /// Gets the full name of the user.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Records a successful login and resets failed attempts.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AccessFailedCount = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft deletes the user.
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the user can login.
    /// </summary>
    /// <returns>True if the user can login, false otherwise</returns>
    public bool CanLogin()
    {
        if (!IsActive)
            return false;

        if (IsDeleted)
            return false;

        if (LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow)
            return false;

        if (!EmailConfirmed && RequireEmailConfirmation())
            return false;

        return true;
    }

    /// <summary>
    /// Override this in derived classes to enforce email confirmation.
    /// </summary>
    protected virtual bool RequireEmailConfirmation() => false;
}
