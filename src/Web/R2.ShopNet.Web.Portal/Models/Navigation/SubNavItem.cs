namespace R2.ShopNet.Web.Portal.Models.Navigation;

/// <summary>
/// Represents a sub-navigation item in the sidebar menu
/// </summary>
public class SubNavItem
{
    /// <summary>
    /// Display name of the navigation item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Route path for the navigation item
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this item should display a "new" badge
    /// </summary>
    public bool IsNew { get; set; }
}
