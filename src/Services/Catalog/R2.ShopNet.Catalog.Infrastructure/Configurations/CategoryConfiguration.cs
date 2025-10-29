using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using R2.ShopNet.Catalog.Domain.Entities;

namespace R2.ShopNet.Catalog.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for Category entity.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        // Self-referencing relationship for hierarchical categories
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
