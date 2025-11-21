namespace R2.ShopNet.Web.Portal.Services;

/// <summary>
/// Service for managing sidebar state (expanded, collapsed, mobile menu)
/// </summary>
public class SidebarService : ISidebarService
{
    private readonly ILogger<SidebarService> _logger;
    private bool _isExpanded = true;
    private bool _isMobileOpen = false;
    private bool _isHovered = false;

    public event Action? OnChange;

    public SidebarService(ILogger<SidebarService> logger)
    {
        _logger = logger;
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                _logger.LogDebug("Sidebar expanded state changed to {IsExpanded}", value);
                NotifyStateChanged();
            }
        }
    }

    public bool IsMobileOpen
    {
        get => _isMobileOpen;
        private set
        {
            if (_isMobileOpen != value)
            {
                _isMobileOpen = value;
                _logger.LogDebug("Mobile sidebar state changed to {IsMobileOpen}", value);
                NotifyStateChanged();
            }
        }
    }

    public bool IsHovered
    {
        get => _isHovered;
        private set
        {
            if (_isHovered != value)
            {
                _isHovered = value;
                NotifyStateChanged();
            }
        }
    }

    public void SetExpanded(bool value)
    {
        IsExpanded = value;
    }

    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    public void SetMobileOpen(bool value)
    {
        IsMobileOpen = value;
    }

    public void ToggleMobileOpen()
    {
        IsMobileOpen = !IsMobileOpen;
    }

    public void SetHovered(bool value)
    {
        IsHovered = value;
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
