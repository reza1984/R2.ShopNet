namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Represents a query that returns a result of type TResponse.
/// </summary>
public interface IQuery<out TResponse>
{
}
