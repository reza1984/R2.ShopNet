namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Interface for entities that support audit tracking.
/// </summary>
public interface IAuditableEntity
{
    string? CreatedBy { get; }
    DateTime CreatedAt { get; }
    string? UpdatedBy { get; }
    DateTime? UpdatedAt { get; }
}
