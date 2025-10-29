using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Domain event raised when a new product is created.
/// </summary>
public record ProductCreatedEvent : BaseEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;

    public ProductCreatedEvent(Guid productId, string name, string sku, Guid categoryId, decimal price, string currency)
    {
        ProductId = productId;
        Name = name;
        Sku = sku;
        CategoryId = categoryId;
        Price = price;
        Currency = currency;
    }
}
