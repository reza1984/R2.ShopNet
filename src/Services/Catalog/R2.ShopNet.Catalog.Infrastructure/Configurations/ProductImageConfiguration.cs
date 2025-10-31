using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using R2.ShopNet.Catalog.Domain.Entities;

namespace R2.ShopNet.Catalog.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for ProductImage entity.
/// </summary>
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(pi => pi.Id);

        // File metadata fields (inherited from FileEntity)
        builder.Property(pi => pi.ObjectKey)
            .IsRequired()
            .HasMaxLength(1000)
            .HasComment("MinIO object key (full path in bucket)");

        builder.Property(pi => pi.FileName)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Original filename");

        builder.Property(pi => pi.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("MIME type (e.g., image/jpeg)");

        builder.Property(pi => pi.SizeInBytes)
            .IsRequired()
            .HasComment("File size in bytes");

        builder.Property(pi => pi.AltText)
            .HasMaxLength(200)
            .HasComment("Alternative text for accessibility");

        builder.Property(pi => pi.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Display order for sorting");

        builder.Property(pi => pi.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this is the primary image");

        // Audit fields (inherited from AuditableSoftDeletableEntity via FileEntity)
        builder.Property(pi => pi.CreatedBy)
            .HasMaxLength(100);

        builder.Property(pi => pi.CreatedAt)
            .IsRequired();

        builder.Property(pi => pi.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(pi => pi.UpdatedAt);

        // Soft delete fields
        builder.Property(pi => pi.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pi => pi.DeletedBy)
            .HasMaxLength(100);

        builder.Property(pi => pi.DeletedAt);

        // Apply query filter for soft delete
        builder.HasQueryFilter(pi => !pi.IsDeleted);

        // Indexes for performance
        builder.HasIndex(pi => pi.ProductId);
        builder.HasIndex(pi => pi.ObjectKey).IsUnique();
        builder.HasIndex(pi => pi.CreatedAt);
        builder.HasIndex(pi => pi.IsDeleted);
        builder.HasIndex(pi => new { pi.ProductId, pi.DisplayOrder });
        builder.HasIndex(pi => new { pi.ProductId, pi.IsPrimary });
    }
}
