namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Dispatches queries to their respective handlers.
/// </summary>
public interface IQueryDispatcher
{
    Task<TResponse> Dispatch<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
