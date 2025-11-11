using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Event raised when a product is updated.
/// </summary>
public record ProductUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;

    public ProductUpdatedEvent(Guid productId, string name, string sku)
    {
        ProductId = productId;
        Name = name;
        Sku = sku;
    }
}
