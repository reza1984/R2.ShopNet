namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides identity using GUID Version 7 (time-ordered UUIDs).
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity using GUID Version 7 (RFC 9562).
    /// </summary>
    public Guid Id { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.CreateVersion7();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(BaseEntity? a, BaseEntity? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(BaseEntity? a, BaseEntity? b)
    {
        return !(a == b);
    }
}
