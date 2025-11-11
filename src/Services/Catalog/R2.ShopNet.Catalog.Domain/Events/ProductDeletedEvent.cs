using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Event raised when a product is deleted (soft delete).
/// </summary>
public record ProductDeletedEvent : BaseEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;

    public ProductDeletedEvent(Guid productId, string name, string sku)
    {
        ProductId = productId;
        Name = name;
        Sku = sku;
    }
}
