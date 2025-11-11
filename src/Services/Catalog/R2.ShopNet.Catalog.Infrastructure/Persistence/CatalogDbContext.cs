using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.Persistence;

namespace R2.ShopNet.Catalog.Infrastructure.Persistence;

/// <summary>
/// Database context for Catalog service.
/// </summary>
public class CatalogDbContext : DbContextBase
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImage> CategoryImages => Set<CategoryImage>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("catalog");

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
