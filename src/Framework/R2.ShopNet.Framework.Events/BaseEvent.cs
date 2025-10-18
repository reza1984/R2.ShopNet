namespace R2.ShopNet.Framework.Events;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract record BaseEvent : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
