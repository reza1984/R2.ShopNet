using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.CQRS;

/// <summary>
/// Default implementation of ICommandDispatcher using IServiceProvider for handler resolution.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Dispatch<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);

        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.Handle));
        if (method == null)
            throw new InvalidOperationException($"Handler method not found for {command.GetType().Name}");

        var result = method.Invoke(handler, new object[] { command, cancellationToken });
        if (result is Task<TResponse> task)
            return await task;

        throw new InvalidOperationException($"Handler did not return Task<{typeof(TResponse).Name}>");
    }

    public async Task<Result> Dispatch(ICommand command, CancellationToken cancellationToken = default)
    {
        return await Dispatch<Result>(command, cancellationToken);
    }
}
