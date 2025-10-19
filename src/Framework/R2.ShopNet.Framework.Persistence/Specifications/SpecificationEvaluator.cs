using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.Persistence.Specifications;

/// <summary>
/// Evaluates specifications and applies them to IQueryable.
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> specification) where TEntity : BaseEntity
    {
        var query = inputQuery;

        // Apply no tracking if specified
        if (specification.IsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply IgnoreQueryFilters if IncludeDeleted is true
        // This allows querying soft-deleted entities when needed
        if (specification.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply include strings
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply paging
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
