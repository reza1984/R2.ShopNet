namespace R2.ShopNet.Catalog.Domain.Enums;

/// <summary>
/// Represents the current status of a product in the catalog.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is in draft mode and not visible to customers.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Product is active and available for purchase.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Product is inactive and not available for purchase.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Product is out of stock.
    /// </summary>
    OutOfStock = 3,

    /// <summary>
    /// Product has been discontinued.
    /// </summary>
    Discontinued = 4
}
