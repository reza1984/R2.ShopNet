namespace R2.ShopNet.Web.Portal.Services;

/// <summary>
/// Service for managing sidebar state (expanded, collapsed, mobile menu)
/// </summary>
public interface ISidebarService
{
    /// <summary>
    /// Event triggered when sidebar state changes
    /// </summary>
    event Action? OnChange;

    /// <summary>
    /// Gets whether the sidebar is expanded on desktop
    /// </summary>
    bool IsExpanded { get; }

    /// <summary>
    /// Gets whether the mobile sidebar menu is open
    /// </summary>
    bool IsMobileOpen { get; }

    /// <summary>
    /// Gets whether the sidebar is being hovered (for collapsed state)
    /// </summary>
    bool IsHovered { get; }

    /// <summary>
    /// Sets the expanded state
    /// </summary>
    void SetExpanded(bool value);

    /// <summary>
    /// Toggles the expanded state
    /// </summary>
    void ToggleExpanded();

    /// <summary>
    /// Sets the mobile menu open state
    /// </summary>
    void SetMobileOpen(bool value);

    /// <summary>
    /// Toggles the mobile menu open state
    /// </summary>
    void ToggleMobileOpen();

    /// <summary>
    /// Sets the hovered state
    /// </summary>
    void SetHovered(bool value);
}
