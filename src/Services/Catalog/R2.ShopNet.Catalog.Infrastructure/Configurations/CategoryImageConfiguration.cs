using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using R2.ShopNet.Catalog.Domain.Entities;

namespace R2.ShopNet.Catalog.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for CategoryImage entity.
/// </summary>
public class CategoryImageConfiguration : IEntityTypeConfiguration<CategoryImage>
{
    public void Configure(EntityTypeBuilder<CategoryImage> builder)
    {
        builder.ToTable("CategoryImages");

        builder.HasKey(ci => ci.Id);

        // File metadata fields (inherited from FileEntity)
        builder.Property(ci => ci.ObjectKey)
            .IsRequired()
            .HasMaxLength(1000)
            .HasComment("MinIO object key (full path in bucket)");

        builder.Property(ci => ci.FileName)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Original filename");

        builder.Property(ci => ci.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("MIME type (e.g., image/jpeg)");

        builder.Property(ci => ci.SizeInBytes)
            .IsRequired()
            .HasComment("File size in bytes");

        builder.Property(ci => ci.AltText)
            .HasMaxLength(200)
            .HasComment("Alternative text for accessibility");

        // Audit fields (inherited from AuditableSoftDeletableEntity via FileEntity)
        builder.Property(ci => ci.CreatedBy)
            .HasMaxLength(100);

        builder.Property(ci => ci.CreatedAt)
            .IsRequired();

        builder.Property(ci => ci.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(ci => ci.UpdatedAt);

        // Soft delete fields
        builder.Property(ci => ci.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ci => ci.DeletedBy)
            .HasMaxLength(100);

        builder.Property(ci => ci.DeletedAt);

        // Apply query filter for soft delete
        builder.HasQueryFilter(ci => !ci.IsDeleted);

        // Relationship with Category
        builder.HasOne(ci => ci.Category)
            .WithMany()
            .HasForeignKey(ci => ci.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(ci => ci.CategoryId);
        builder.HasIndex(ci => ci.ObjectKey).IsUnique();
        builder.HasIndex(ci => ci.CreatedAt);
        builder.HasIndex(ci => ci.IsDeleted);
    }
}
