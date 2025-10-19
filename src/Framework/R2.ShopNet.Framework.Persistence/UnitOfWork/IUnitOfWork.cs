using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.Persistence.Repositories;

namespace R2.ShopNet.Framework.Persistence.UnitOfWork;

/// <summary>
/// Unit of Work interface for managing transactions and repository access.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from BaseEntity</typeparam>
    /// <returns>Repository instance</returns>
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// Gets a read-only repository for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from BaseEntity</typeparam>
    /// <returns>Read-only repository instance</returns>
    IReadOnlyRepository<TEntity> ReadOnlyRepository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function within a transaction scope.
    /// Automatically commits on success or rolls back on exception.
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="action">Function to execute within transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the function</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
}
