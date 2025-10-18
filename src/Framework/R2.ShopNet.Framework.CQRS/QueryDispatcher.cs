using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Default implementation of IQueryDispatcher using IServiceProvider for handler resolution.
/// </summary>
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Dispatch<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);

        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.Handle));
        if (method == null)
            throw new InvalidOperationException($"Handler method not found for {query.GetType().Name}");

        var result = method.Invoke(handler, new object[] { query, cancellationToken });
        if (result is Task<TResponse> task)
            return await task;

        throw new InvalidOperationException($"Handler did not return Task<{typeof(TResponse).Name}>");
    }
}
