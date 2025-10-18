namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Handles a query of type TQuery and returns a result of type TResponse.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
}
