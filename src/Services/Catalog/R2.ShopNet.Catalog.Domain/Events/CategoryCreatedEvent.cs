using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Event raised when a new category is created.
/// </summary>
public record CategoryCreatedEvent : BaseEvent
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid? ParentCategoryId { get; init; }

    public CategoryCreatedEvent(Guid categoryId, string name, string slug, Guid? parentCategoryId = null)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
        ParentCategoryId = parentCategoryId;
    }
}
