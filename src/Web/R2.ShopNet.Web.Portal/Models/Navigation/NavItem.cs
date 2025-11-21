namespace R2.ShopNet.Web.Portal.Models.Navigation;

/// <summary>
/// Represents a main navigation item in the sidebar menu
/// </summary>
public class NavItem
{
    /// <summary>
    /// Display name of the navigation item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Material icon name for the navigation item
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Route path for the navigation item (null if this is a parent with sub-items)
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Indicates if this item should display a "new" badge
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Child navigation items (if this is a parent menu item)
    /// </summary>
    public List<SubNavItem>? SubItems { get; set; }
}
