using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Framework.Persistence;

/// <summary>
/// Base DbContext that automatically applies global query filters for soft-deletable entities.
/// All entities implementing ISoftDeletable will be automatically filtered unless IgnoreQueryFilters() is used.
/// </summary>
public abstract class DbContextBase : DbContext
{
    protected DbContextBase(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for all ISoftDeletable entities
        ApplySoftDeleteQueryFilter(modelBuilder);
    }

    /// <summary>
    /// Applies global query filter to automatically exclude soft-deleted entities.
    /// This filter applies to all entities implementing ISoftDeletable.
    /// </summary>
    private void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if the entity implements ISoftDeletable
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                // Create the filter expression: e => !e.IsDeleted
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Not(property),
                    parameter);

                // Apply the filter
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }
}
