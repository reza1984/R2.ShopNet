namespace R2.ShopNet.Catalog.Application.DTOs;

/// <summary>
/// Data transfer object for Product.
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? DiscountPrice { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public int StockQuantity { get; init; }
    public int ReorderLevel { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? Brand { get; init; }
    public decimal? Weight { get; init; }
    public string? Dimensions { get; init; }
    public List<ProductImageDto> Images { get; init; } = [];
    public List<ProductVariantDto> Variants { get; init; } = [];
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaKeywords { get; init; }
    public int ViewCount { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Data transfer object for Product Image.
/// </summary>
public record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
    public string? AltText { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>
/// Data transfer object for Product Variant.
/// </summary>
public record ProductVariantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public int StockQuantity { get; init; }
    public decimal? Weight { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = new();
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
}
