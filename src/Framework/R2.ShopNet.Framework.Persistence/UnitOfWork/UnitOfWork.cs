using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.Persistence.Repositories;
using R2.ShopNet.Framework.Persistence.Auditing;

namespace R2.ShopNet.Framework.Persistence.UnitOfWork;

/// <summary>
/// Unit of Work implementation using Entity Framework Core.
/// Manages DbContext lifecycle and provides repository access.
/// Automatically handles audit tracking for IAuditableEntity implementations.
/// Automatically converts hard deletes to soft deletes for ISoftDeletable entities.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private readonly Dictionary<Type, object> _readOnlyRepositories;
    private readonly ICurrentUserAccessor? _currentUserAccessor;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(DbContext context, ICurrentUserAccessor? currentUserAccessor = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserAccessor = currentUserAccessor;
        _repositories = new Dictionary<Type, object>();
        _readOnlyRepositories = new Dictionary<Type, object>();
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);

        if (_repositories.ContainsKey(type))
        {
            return (IRepository<TEntity>)_repositories[type];
        }

        var repositoryInstance = new Repository<TEntity>(_context);
        _repositories.Add(type, repositoryInstance);

        return repositoryInstance;
    }

    public IReadOnlyRepository<TEntity> ReadOnlyRepository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);

        if (_readOnlyRepositories.ContainsKey(type))
        {
            return (IReadOnlyRepository<TEntity>)_readOnlyRepositories[type];
        }

        var repositoryInstance = new ReadOnlyRepository<TEntity>(_context);
        _readOnlyRepositories.Add(type, repositoryInstance);

        return repositoryInstance;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle audit tracking for IAuditableEntity
        HandleAuditableEntities();

        // Convert hard deletes to soft deletes for ISoftDeletable entities
        HandleSoftDeletableEntities();

        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically converts hard deletes to soft deletes for entities implementing ISoftDeletable.
    /// When an entity marked for deletion implements ISoftDeletable, instead of deleting it,
    /// we set IsDeleted = true, DeletedBy, DeletedAt and change the state to Modified.
    /// </summary>
    private void HandleSoftDeletableEntities()
    {
        var currentUser = _currentUserAccessor?.GetCurrentUserId() ?? "System";
        var now = DateTime.UtcNow;

        var deletedEntries = _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
            .ToList();

        foreach (var entry in deletedEntries)
        {
            var softDeletableEntity = (ISoftDeletable)entry.Entity;

            // Convert hard delete to soft delete
            entry.State = EntityState.Modified;
            softDeletableEntity.IsDeleted = true;
            softDeletableEntity.DeletedBy = currentUser;
            softDeletableEntity.DeletedAt = now;
        }
    }

    /// <summary>
    /// Automatically sets audit fields for entities implementing IAuditableEntity.
    /// </summary>
    private void HandleAuditableEntities()
    {
        var currentUser = _currentUserAccessor?.GetCurrentUserId() ?? "System";
        var now = DateTime.UtcNow;

        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var auditableEntity = (IAuditableEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                // Set creation audit fields
                auditableEntity.CreatedBy = currentUser;
                auditableEntity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                // Set update audit fields
                auditableEntity.UpdatedBy = currentUser;
                auditableEntity.UpdatedAt = now;
            }
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        await BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _repositories.Clear();
                _readOnlyRepositories.Clear();
                // Note: DbContext is NOT disposed here as it's managed by DI container
            }

            _disposed = true;
        }
    }
}
