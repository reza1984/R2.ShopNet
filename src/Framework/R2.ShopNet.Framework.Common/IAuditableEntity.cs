namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Interface for entities that support audit tracking.
/// Entities implementing this interface will have audit fields automatically populated
/// when saved through the Unit of Work pattern.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// The user who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// The UTC date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// The user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; set; }

    /// <summary>
    /// The UTC date and time when the entity was last updated.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
