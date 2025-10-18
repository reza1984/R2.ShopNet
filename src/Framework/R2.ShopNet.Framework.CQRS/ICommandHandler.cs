using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Handles a command of type TCommand and returns a result of type TResponse.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a command of type TCommand that doesn't return a value.
/// </summary>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Result>
    where TCommand : ICommand
{
}
