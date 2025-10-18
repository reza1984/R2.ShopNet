using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Marker interface for commands that don't return a value.
/// </summary>
public interface ICommand : ICommand<Result>
{
}

/// <summary>
/// Represents a command that returns a result of type TResponse.
/// </summary>
public interface ICommand<out TResponse>
{
}
