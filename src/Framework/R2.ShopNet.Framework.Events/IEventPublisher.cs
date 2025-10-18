namespace R2.ShopNet.Framework.Events;

/// <summary>
/// Interface for publishing domain events.
/// </summary>
public interface IEventPublisher
{
    Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    Task PublishMany(IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
}
