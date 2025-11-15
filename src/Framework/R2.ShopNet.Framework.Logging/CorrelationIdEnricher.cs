using Serilog.Core;
using Serilog.Events;

namespace R2.ShopNet.Framework.Logging;

/// <summary>
/// Enriches log events with correlation ID for request tracking
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdPropertyName = "CorrelationId";
    private readonly string _correlationId;

    public CorrelationIdEnricher(string correlationId)
    {
        _correlationId = correlationId;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, _correlationId);
        logEvent.AddPropertyIfAbsent(property);
    }
}
