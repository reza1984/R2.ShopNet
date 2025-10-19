using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Persistence.Repositories;
using R2.ShopNet.Framework.Persistence.UnitOfWork;
using R2.ShopNet.Framework.Persistence.Auditing;

namespace R2.ShopNet.Framework.Persistence.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Unit of Work pattern with the specified DbContext.
    /// Automatically resolves ICurrentUserAccessor if registered for audit tracking.
    /// </summary>
    /// <typeparam name="TContext">DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            var currentUserAccessor = provider.GetService<ICurrentUserAccessor>();
            return new UnitOfWork.UnitOfWork(context, currentUserAccessor);
        });

        return services;
    }

    /// <summary>
    /// Registers generic repositories for the specified DbContext.
    /// </summary>
    /// <typeparam name="TContext">DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRepositories<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepository<>));

        return services;
    }

    /// <summary>
    /// Registers a custom repository implementation.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TRepository">Repository implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRepository<TEntity, TRepository>(this IServiceCollection services)
        where TEntity : BaseEntity
        where TRepository : class, IRepository<TEntity>
    {
        services.AddScoped<IRepository<TEntity>, TRepository>();
        return services;
    }

    /// <summary>
    /// Registers a custom read-only repository implementation.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TRepository">Repository implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddReadOnlyRepository<TEntity, TRepository>(this IServiceCollection services)
        where TEntity : BaseEntity
        where TRepository : class, IReadOnlyRepository<TEntity>
    {
        services.AddScoped<IReadOnlyRepository<TEntity>, TRepository>();
        return services;
    }

    /// <summary>
    /// Registers both Unit of Work and generic repositories for the specified DbContext.
    /// This is the recommended method for setting up persistence infrastructure.
    /// </summary>
    /// <typeparam name="TContext">DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPersistence<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddUnitOfWork<TContext>();
        services.AddRepositories<TContext>();

        return services;
    }

    /// <summary>
    /// Registers the default HttpContext-based current user accessor for audit tracking.
    /// NOTE: This requires HttpContextAccessor - ensure you call services.AddHttpContextAccessor() first!
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCurrentUserAccessor(this IServiceCollection services)
    {
        // Note: HttpContextAccessor must be registered by the application
        // Call services.AddHttpContextAccessor() in Program.cs before this method
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

        return services;
    }

    /// <summary>
    /// Registers a custom current user accessor implementation for audit tracking.
    /// </summary>
    /// <typeparam name="TImplementation">Custom ICurrentUserAccessor implementation</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCurrentUserAccessor<TImplementation>(this IServiceCollection services)
        where TImplementation : class, ICurrentUserAccessor
    {
        services.AddScoped<ICurrentUserAccessor, TImplementation>();

        return services;
    }

    /// <summary>
    /// Registers the audit log store for persisting command execution audit logs.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAuditLogStore(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogStore, AuditLogStore>();
        return services;
    }

    /// <summary>
    /// Registers command auditing decorator to automatically log all command executions.
    /// This wraps all ICommandHandler implementations with auditing capabilities.
    /// NOTE: Ensure you register IAuditLogStore before calling this method.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCommandAuditing(this IServiceCollection services)
    {
        // Register the audit log store if not already registered
        services.AddAuditLogStore();

        // Decorate all command handlers with auditing
        services.Decorate(typeof(ICommandHandler<,>), typeof(AuditingCommandHandlerDecorator<,>));

        return services;
    }

    /// <summary>
    /// Registers complete audit infrastructure including audit log store and command auditing.
    /// This is the recommended method for enabling full audit tracking.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAuditing(this IServiceCollection services)
    {
        services.AddAuditLogStore();
        services.AddCommandAuditing();

        return services;
    }
}
