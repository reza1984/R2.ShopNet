using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Identity.Infrastructure.Events;

/// <summary>
/// In-memory implementation of IEventPublisher for development/testing.
/// TODO: Replace with a proper message broker (RabbitMQ, Kafka, etc.) in production.
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        _logger.LogInformation("Publishing event in-memory: {EventType} - {EventId}", 
            @event.GetType().Name, @event.EventId);
        
        // In a real implementation, this would publish to a message broker
        // For now, we just log it
        return Task.CompletedTask;
    }

    public Task PublishMany(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            _logger.LogInformation("Publishing event in-memory: {EventType} - {EventId}", 
                @event.GetType().Name, @event.EventId);
        }
        
        // In a real implementation, this would publish to a message broker
        // For now, we just log them
        return Task.CompletedTask;
    }
}
