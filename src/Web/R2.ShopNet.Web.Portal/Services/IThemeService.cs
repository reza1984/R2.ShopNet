namespace R2.ShopNet.Web.Portal.Services;

/// <summary>
/// Service for managing application theme (light/dark mode)
/// Supports system preference detection and manual user override
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Event triggered when theme changes
    /// </summary>
    event Action? OnChange;

    /// <summary>
    /// Gets the current theme
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets the detected system theme preference
    /// </summary>
    ThemeMode? SystemPreference { get; set; }

    /// <summary>
    /// Sets the theme
    /// </summary>
    void SetTheme(ThemeMode theme);

    /// <summary>
    /// Toggles between light and dark theme
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// Gets the current theme as a string ("light" or "dark")
    /// </summary>
    string GetThemeString();

    /// <summary>
    /// Gets the system preference as a theme enum
    /// </summary>
    ThemeMode GetSystemPreference();

    /// <summary>
    /// Resets to system preference by clearing stored theme
    /// This allows the theme to follow OS changes automatically
    /// </summary>
    void ResetToSystemPreference();
}

/// <summary>
/// Theme mode enumeration
/// </summary>
public enum ThemeMode
{
    Light,
    Dark
}
