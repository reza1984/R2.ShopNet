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

        builder.Property(pi => pi.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pi => pi.AltText)
            .HasMaxLength(200);

        builder.Property(pi => pi.DisplayOrder)
            .IsRequired();

        builder.Property(pi => pi.IsPrimary)
            .IsRequired();
    }
}
