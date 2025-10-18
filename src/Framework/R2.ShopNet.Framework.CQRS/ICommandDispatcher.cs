using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Dispatches commands to their respective handlers.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResponse> Dispatch<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task<Result> Dispatch(ICommand command, CancellationToken cancellationToken = default);
}
