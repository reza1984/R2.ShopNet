using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.ValueObjects;

namespace R2.ShopNet.Catalog.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for Product entity.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(5000);

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        // Configure Money value object for Price
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure Money value object for DiscountPrice
        builder.OwnsOne(p => p.DiscountPrice, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("DiscountPrice")
                .HasPrecision(18, 2);

            price.Property(m => m.Currency)
                .HasColumnName("DiscountCurrency")
                .HasMaxLength(3);
        });

        // Configure Money value object for CostPrice
        builder.OwnsOne(p => p.CostPrice, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("CostPrice")
                .HasPrecision(18, 2);

            price.Property(m => m.Currency)
                .HasColumnName("CostCurrency")
                .HasMaxLength(3);
        });

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.ReorderLevel)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Brand)
            .HasMaxLength(100);

        builder.Property(p => p.Weight)
            .HasPrecision(10, 2);

        builder.Property(p => p.Dimensions)
            .HasMaxLength(50);

        builder.Property(p => p.MetaTitle)
            .HasMaxLength(60);

        builder.Property(p => p.MetaDescription)
            .HasMaxLength(160);

        builder.Property(p => p.MetaKeywords)
            .HasMaxLength(255);

        builder.Property(p => p.AverageRating)
            .HasPrecision(3, 2);

        // Relationship with Category
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with ProductImages
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with ProductVariants
        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(pv => pv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
