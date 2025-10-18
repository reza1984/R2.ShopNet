namespace R2.ShopNet.Framework.Events;

/// <summary>
/// Interface for event handlers.
/// </summary>
public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
