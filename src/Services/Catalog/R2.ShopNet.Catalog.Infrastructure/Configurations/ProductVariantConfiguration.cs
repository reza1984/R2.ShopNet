using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.ValueObjects;
using System.Text.Json;

namespace R2.ShopNet.Catalog.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for ProductVariant entity.
/// </summary>
public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pv => pv.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(pv => pv.Sku)
            .IsUnique();

        // Configure Money value object for Price
        builder.OwnsOne(pv => pv.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2);

            price.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.Property(pv => pv.StockQuantity)
            .IsRequired();

        builder.Property(pv => pv.Weight)
            .HasPrecision(10, 2);

        builder.Property(pv => pv.ImageUrl)
            .HasMaxLength(500);

        builder.Property(pv => pv.IsActive)
            .IsRequired();

        // Store Attributes dictionary as JSON
        builder.Property(pv => pv.Attributes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("jsonb");
    }
}
