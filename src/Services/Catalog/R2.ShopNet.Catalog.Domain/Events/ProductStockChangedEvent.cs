using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.Domain.Events;

/// <summary>
/// Domain event raised when product stock quantity changes.
/// </summary>
public record ProductStockChangedEvent : BaseEvent
{
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public int OldQuantity { get; init; }
    public int NewQuantity { get; init; }
    public string Reason { get; init; } = string.Empty;

    public ProductStockChangedEvent(Guid productId, string sku, int oldQuantity, int newQuantity, string reason)
    {
        ProductId = productId;
        Sku = sku;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
        Reason = reason;
    }
}
