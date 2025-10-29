using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Framework.CQRS.DependencyInjection;

/// <summary>
/// Extension methods for registering CQRS handlers automatically via assembly scanning.
/// Similar to MediatR's registration approach.
/// </summary>
public static class CQRSServiceCollectionExtensions
{
    /// <summary>
    /// Registers all command and query handlers from the specified assemblies.
    /// Scans assemblies for types implementing ICommandHandler or IQueryHandler and registers them.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCQRSHandlers(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));
        }

        // Register dispatchers
        services.Add(new ServiceDescriptor(typeof(ICommandDispatcher), typeof(CommandDispatcher), lifetime));
        services.Add(new ServiceDescriptor(typeof(IQueryDispatcher), typeof(QueryDispatcher), lifetime));

        // Scan assemblies and register handlers
        foreach (var assembly in assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, lifetime);
        }

        return services;
    }

    /// <summary>
    /// Registers all command and query handlers from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="TAssemblyMarker">Type from the assembly to scan</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCQRSHandlersFromAssemblyContaining<TAssemblyMarker>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddCQRSHandlers(lifetime, typeof(TAssemblyMarker).Assembly);
    }

    /// <summary>
    /// Registers all command and query handlers from the calling assembly.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCQRSHandlersFromCallingAssembly(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddCQRSHandlers(lifetime, Assembly.GetCallingAssembly());
    }

    private static void RegisterHandlersFromAssembly(
        IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            // Find all interfaces that this type implements
            var interfaces = handlerType.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                // Only process generic interfaces
                if (!@interface.IsGenericType)
                    continue;

                var genericDefinition = @interface.GetGenericTypeDefinition();

                // Register ICommandHandler<TCommand, TResponse>
                if (genericDefinition == typeof(ICommandHandler<,>))
                {
                    services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
                }
                // Register IQueryHandler<TQuery, TResponse>
                else if (genericDefinition == typeof(IQueryHandler<,>))
                {
                    services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
                }
            }
        }
    }

    /// <summary>
    /// Gets statistics about registered handlers for debugging purposes.
    /// </summary>
    public static CQRSRegistrationStats GetHandlerStats(this IServiceCollection services)
    {
        var commandHandlers = services.Count(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

        var queryHandlers = services.Count(s =>
            s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

        return new CQRSRegistrationStats
        {
            CommandHandlerCount = commandHandlers,
            QueryHandlerCount = queryHandlers,
            TotalHandlerCount = commandHandlers + queryHandlers
        };
    }
}

/// <summary>
/// Statistics about registered CQRS handlers.
/// </summary>
public record CQRSRegistrationStats
{
    public int CommandHandlerCount { get; init; }
    public int QueryHandlerCount { get; init; }
    public int TotalHandlerCount { get; init; }
}
