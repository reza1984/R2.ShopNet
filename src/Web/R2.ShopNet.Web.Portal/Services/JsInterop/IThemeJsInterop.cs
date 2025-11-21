namespace R2.ShopNet.Web.Portal.Services.JsInterop;

/// <summary>
/// JavaScript interop service for theme management
/// </summary>
public interface IThemeJsInterop
{
    /// <summary>
    /// Detects if the user's system prefers dark mode
    /// </summary>
    Task<bool> GetSystemPrefersDarkModeAsync();

    /// <summary>
    /// Gets the saved theme preference from localStorage
    /// </summary>
    Task<string?> GetSavedThemeAsync();

    /// <summary>
    /// Saves the theme preference to localStorage
    /// </summary>
    Task SaveThemeAsync(string theme);

    /// <summary>
    /// Removes the saved theme preference from localStorage
    /// </summary>
    Task RemoveThemeAsync();

    /// <summary>
    /// Applies the dark mode class to the document
    /// </summary>
    Task ApplyDarkModeAsync();

    /// <summary>
    /// Removes the dark mode class from the document
    /// </summary>
    Task RemoveDarkModeAsync();

    /// <summary>
    /// Sets up a listener for system theme changes
    /// </summary>
    Task ListenForSystemThemeChangesAsync();
}
