namespace R2.ShopNet.Web.Portal.Services;

public class SidebarService
{
    private bool _isExpanded = true;
    private bool _isMobileOpen = false;
    private bool _isHovered = false;

    public event Action? OnChange;

    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
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
