namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Base class for domain entities that require audit tracking.
/// Implements IAuditableEntity to automatically populate audit fields via the Unit of Work pattern.
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public string? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    protected AuditableEntity(Guid id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
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
