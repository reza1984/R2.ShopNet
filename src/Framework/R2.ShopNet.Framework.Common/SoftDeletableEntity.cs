namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Base class for domain entities that only require soft delete functionality without audit tracking.
/// Implements ISoftDeletable to support soft delete operations.
/// </summary>
public abstract class SoftDeletableEntity : BaseEntity, ISoftDeletable
{
    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    protected SoftDeletableEntity()
    {
        IsDeleted = false;
    }

    protected SoftDeletableEntity(Guid id) : base(id)
    {
        IsDeleted = false;
    }

    /// <summary>
    /// Marks the entity as soft deleted.
    /// Sets IsDeleted to true and records when it was deleted.
    /// Note: DeletedBy will be automatically set by the UnitOfWork.
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft deleted entity.
    /// Sets IsDeleted to false and clears deletion tracking.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedBy = null;
        DeletedAt = null;
    }
}
