using System.Linq.Expressions;

namespace R2.ShopNet.Framework.Persistence.Specifications;

/// <summary>
/// Specification pattern interface for encapsulating query logic.
/// Specifications allow for reusable, composable, and testable query definitions.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the filter criteria for the specification.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the list of include expressions for eager loading.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of include string expressions for eager loading (useful for ThenInclude).
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by descending expression.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the group by expression.
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Gets the number of items to take.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Indicates if change tracking should be disabled (AsNoTracking).
    /// </summary>
    bool IsNoTracking { get; }

    /// <summary>
    /// Indicates if soft-deleted entities should be included.
    /// </summary>
    bool IncludeDeleted { get; }
}
