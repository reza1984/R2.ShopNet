using System.Linq.Expressions;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.Persistence.Specifications;

namespace R2.ShopNet.Framework.Persistence.Repositories;

/// <summary>
/// Generic repository interface for CRUD operations on entities.
/// </summary>
/// <typeparam name="TEntity">Entity type that inherits from BaseEntity</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    // Query Operations
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(Guid id, bool includeDeleted, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    // Pagination
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        ISpecification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Existence and Count
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    // Command Operations
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    // Soft Delete Operations
    Task SoftDeleteAsync(TEntity entity, string currentUserId, CancellationToken cancellationToken = default);
    Task<TEntity?> SoftDeleteAsync(Guid id, string currentUserId, CancellationToken cancellationToken = default);
    Task SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    // Queryable (for advanced scenarios)
    IQueryable<TEntity> AsQueryable();
}
