namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Interface for entities that support soft delete functionality.
/// Entities implementing this interface can be marked as deleted without being physically removed from the database.
/// Tracks who deleted the entity and when.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted.
    /// When true, the entity is logically deleted but still exists in the database.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// The user who deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }

    /// <summary>
    /// The UTC date and time when the entity was deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
