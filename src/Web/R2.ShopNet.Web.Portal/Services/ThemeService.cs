namespace R2.ShopNet.Web.Portal.Services;

/// <summary>
/// Service for managing application theme (light/dark mode)
/// Supports system preference detection and manual user override
/// </summary>
public class ThemeService
{
    public enum Theme
    {
        Light,
        Dark
    }

    private Theme _currentTheme = Theme.Light;
    private Theme? _systemPreference;

    public event Action? OnChange;

    public Theme CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// The detected system theme preference
    /// </summary>
    public Theme? SystemPreference
    {
        get => _systemPreference;
        set => _systemPreference = value; 
    }

    /// <summary>
    /// Sets the theme
    /// </summary>
    public void SetTheme(Theme theme)
    {
        CurrentTheme = theme;
    }

    /// <summary>
    /// Toggles between light and dark theme
    /// </summary>
    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light;
    }

    /// <summary>
    /// Gets the current theme as a string ("light" or "dark")
    /// </summary>
    public string GetThemeString()
    {
        return CurrentTheme == Theme.Light ? "light" : "dark";
    }

    /// <summary>
    /// Gets the system preference as a theme enum
    /// </summary>
    public Theme GetSystemPreference()
    {
        return SystemPreference ?? Theme.Light;
    }

    /// <summary>
    /// Resets to system preference by clearing stored theme
    /// This allows the theme to follow OS changes automatically
    /// </summary>
    public void ResetToSystemPreference()
    {
        if (SystemPreference.HasValue)
        {
            CurrentTheme = SystemPreference.Value;
        }
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
