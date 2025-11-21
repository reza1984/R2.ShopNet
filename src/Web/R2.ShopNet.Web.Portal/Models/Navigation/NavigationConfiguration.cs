namespace R2.ShopNet.Web.Portal.Models.Navigation;

/// <summary>
/// Configuration for the application navigation menu
/// </summary>
public class NavigationConfiguration
{
    /// <summary>
    /// Main navigation menu items
    /// </summary>
    public List<NavItem> MainMenu { get; set; } = new();

    /// <summary>
    /// Secondary navigation menu items (typically settings, admin, etc.)
    /// </summary>
    public List<NavItem> OthersMenu { get; set; } = new();

    /// <summary>
    /// Gets the default navigation configuration
    /// </summary>
    public static NavigationConfiguration GetDefault()
    {
        return new NavigationConfiguration
        {
            MainMenu = new()
            {
                new() { Icon = "dashboard", Name = "Dashboard", Path = "/dashboard" },
                new()
                {
                    Icon = "person",
                    Name = "Users",
                    SubItems = new()
                    {
                        new() { Name = "All Users", Path = "/users" },
                        new() { Name = "Roles & Permissions", Path = "/users/roles" }
                    }
                },
                new()
                {
                    Icon = "category",
                    Name = "Catalog",
                    SubItems = new()
                    {
                        new() { Name = "Products", Path = "/catalog/products" },
                        new() { Name = "Categories", Path = "/catalog/categories" },
                        new() { Name = "Inventory", Path = "/catalog/inventory" }
                    }
                },
                new()
                {
                    Icon = "shopping_bag",
                    Name = "Orders",
                    SubItems = new()
                    {
                        new() { Name = "All Orders", Path = "/orders" },
                        new() { Name = "Pending", Path = "/orders/pending" },
                        new() { Name = "Completed", Path = "/orders/completed" }
                    }
                },
                new() { Icon = "calendar_month", Name = "Reports", Path = "/reports" }
            },
            OthersMenu = new()
            {
                new() { Icon = "analytics", Name = "Analytics", Path = "/analytics" },
                new()
                {
                    Icon = "settings",
                    Name = "Settings",
                    SubItems = new()
                    {
                        new() { Name = "General", Path = "/settings" },
                        new() { Name = "Configuration", Path = "/settings/configuration" }
                    }
                }
            }
        };
    }
}
