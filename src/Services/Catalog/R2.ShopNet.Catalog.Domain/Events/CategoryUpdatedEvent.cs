using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Event raised when a category is updated.
/// </summary>
public record CategoryUpdatedEvent : BaseEvent
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;

    public CategoryUpdatedEvent(Guid categoryId, string name, string slug)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
    }
}
