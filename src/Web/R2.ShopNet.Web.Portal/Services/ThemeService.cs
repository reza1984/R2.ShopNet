namespace R2.ShopNet.Web.Portal.Services;

/// <summary>
/// Service for managing application theme (light/dark mode)
/// Supports system preference detection and manual user override
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private ThemeMode _currentTheme = ThemeMode.Light;
    private ThemeMode? _systemPreference;

    public event Action? OnChange;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    public ThemeMode CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                _logger.LogDebug("Theme changed to {Theme}", value);
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// The detected system theme preference
    /// </summary>
    public ThemeMode? SystemPreference
    {
        get => _systemPreference;
        set => _systemPreference = value;
    }

    /// <summary>
    /// Sets the theme
    /// </summary>
    public void SetTheme(ThemeMode theme)
    {
        CurrentTheme = theme;
    }

    /// <summary>
    /// Toggles between light and dark theme
    /// </summary>
    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
    }

    /// <summary>
    /// Gets the current theme as a string ("light" or "dark")
    /// </summary>
    public string GetThemeString()
    {
        return CurrentTheme == ThemeMode.Light ? "light" : "dark";
    }

    /// <summary>
    /// Gets the system preference as a theme enum
    /// </summary>
    public ThemeMode GetSystemPreference()
    {
        return SystemPreference ?? ThemeMode.Light;
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
