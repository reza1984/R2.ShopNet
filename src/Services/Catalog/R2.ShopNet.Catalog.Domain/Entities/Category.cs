using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Domain.Entities;

/// <summary>
/// Represents a product category in the catalog.
/// </summary>
public class Category : AuditableSoftDeletableEntity
{
    private readonly List<Product> _products = [];
    private readonly List<Category> _subCategories = [];

    /// <summary>
    /// The name of the category.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The description of the category.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The URL-friendly slug for the category.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// The parent category ID for hierarchical categories.
    /// </summary>
    public Guid? ParentCategoryId { get; private set; }

    /// <summary>
    /// The parent category for hierarchical categories.
    /// </summary>
    public Category? ParentCategory { get; private set; }

    /// <summary>
    /// Collection of subcategories.
    /// </summary>
    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

    /// <summary>
    /// Products in this category.
    /// </summary>
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    /// <summary>
    /// Display order for sorting categories.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// URL to the category image.
    /// </summary>
    public string? ImageUrl { get; private set; }

    private Category() { }

    public Category(string name, string slug, string? description = null, Guid? parentCategoryId = null)
    {
        SetName(name);
        SetSlug(slug);
        Description = description;
        ParentCategoryId = parentCategoryId;
        DisplayOrder = 0;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));

        Name = name;
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category slug cannot be empty", nameof(slug));

        if (slug.Length > 150)
            throw new ArgumentException("Category slug cannot exceed 150 characters", nameof(slug));

        Slug = slug.ToLowerInvariant();
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void SetParentCategory(Guid? parentCategoryId)
    {
        ParentCategoryId = parentCategoryId;
    }
}
