namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Base class for domain entities that require both audit tracking and soft delete functionality.
/// Implements IAuditableEntity to automatically populate audit fields via the Unit of Work pattern.
/// Implements ISoftDeletable to support soft delete operations.
/// This is the most common base class for domain entities.
/// </summary>
public abstract class AuditableSoftDeletableEntity : BaseEntity, IAuditableEntity, ISoftDeletable
{
    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public string? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    protected AuditableSoftDeletableEntity()
    {
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    protected AuditableSoftDeletableEntity(Guid id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Marks the entity as soft deleted.
    /// Sets IsDeleted to true and records deletion time.
    /// Note: DeletedBy will be automatically set by the UnitOfWork.
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft deleted entity.
    /// Sets IsDeleted to false, clears deletion tracking, and updates the timestamp.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedBy = null;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the entity's UpdatedAt timestamp.
    /// Call this method when making changes to the entity.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
