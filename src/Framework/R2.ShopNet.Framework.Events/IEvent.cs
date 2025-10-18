namespace R2.ShopNet.Framework.Events;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
