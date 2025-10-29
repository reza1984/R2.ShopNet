namespace R2.ShopNet.Catalog.Application.DTOs;

/// <summary>
/// Data transfer object for Category.
/// </summary>
public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Slug { get; init; } = string.Empty;
    public Guid? ParentCategoryId { get; init; }
    public string? ParentCategoryName { get; init; }
    public int DisplayOrder { get; init; }
    public string? ImageUrl { get; init; }
    public int ProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
