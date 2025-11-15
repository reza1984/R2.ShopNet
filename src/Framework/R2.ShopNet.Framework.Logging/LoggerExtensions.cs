using Serilog;
using Serilog.Context;

namespace R2.ShopNet.Framework.Logging;

/// <summary>
/// Extension methods for ILogger to provide additional logging functionality
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Adds a correlation ID to the log context for the duration of the returned disposable
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="correlationId">The correlation ID to add</param>
    /// <returns>A disposable that removes the correlation ID when disposed</returns>
    public static IDisposable WithCorrelationId(this ILogger logger, string correlationId)
    {
        return LogContext.PushProperty("CorrelationId", correlationId);
    }

    /// <summary>
    /// Adds a user ID to the log context for the duration of the returned disposable
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="userId">The user ID to add</param>
    /// <returns>A disposable that removes the user ID when disposed</returns>
    public static IDisposable WithUserId(this ILogger logger, string userId)
    {
        return LogContext.PushProperty("UserId", userId);
    }

    /// <summary>
    /// Adds a tenant ID to the log context for the duration of the returned disposable
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="tenantId">The tenant ID to add</param>
    /// <returns>A disposable that removes the tenant ID when disposed</returns>
    public static IDisposable WithTenantId(this ILogger logger, string tenantId)
    {
        return LogContext.PushProperty("TenantId", tenantId);
    }

    /// <summary>
    /// Adds custom properties to the log context for the duration of the returned disposable
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="properties">Dictionary of properties to add</param>
    /// <returns>A disposable that removes the properties when disposed</returns>
    public static IDisposable WithProperties(this ILogger logger, IDictionary<string, object> properties)
    {
        var disposables = properties
            .Select(kvp => LogContext.PushProperty(kvp.Key, kvp.Value))
            .ToList();

        return new CompositeDisposable(disposables);
    }

    private class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables;

        public CompositeDisposable(List<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
