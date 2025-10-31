using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Event raised when a category is deleted.
/// </summary>
public record CategoryDeletedEvent : BaseEvent
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;

    public CategoryDeletedEvent(Guid categoryId, string name)
    {
        CategoryId = categoryId;
        Name = name;
    }
}
