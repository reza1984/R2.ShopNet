using Microsoft.JSInterop;

namespace R2.ShopNet.Web.Portal.Services.JsInterop;

/// <summary>
/// JavaScript interop service for theme management
/// </summary>
public class ThemeJsInterop : IThemeJsInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ThemeJsInterop> _logger;

    public ThemeJsInterop(IJSRuntime jsRuntime, ILogger<ThemeJsInterop> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> GetSystemPrefersDarkModeAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("eval",
                "window.matchMedia('(prefers-color-scheme: dark)').matches");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get system theme preference");
            return false;
        }
    }

    public async Task<string?> GetSavedThemeAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("eval", "localStorage.getItem('theme')");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get saved theme from localStorage");
            return null;
        }
    }

    public async Task SaveThemeAsync(string theme)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", $"localStorage.setItem('theme', '{theme}')");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save theme to localStorage");
        }
    }

    public async Task RemoveThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", "localStorage.removeItem('theme')");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove theme from localStorage");
        }
    }

    public async Task ApplyDarkModeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", "document.documentElement.classList.add('dark')");
            await _jsRuntime.InvokeVoidAsync("eval", "document.body.classList.add('dark:bg-gray-900')");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply dark mode");
        }
    }

    public async Task RemoveDarkModeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", "document.documentElement.classList.remove('dark')");
            await _jsRuntime.InvokeVoidAsync("eval", "document.body.classList.remove('dark:bg-gray-900')");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove dark mode");
        }
    }

    public async Task ListenForSystemThemeChangesAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", @"
                window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                    if (!localStorage.getItem('theme')) {
                        const newTheme = e.matches ? 'dark' : 'light';
                        if (newTheme === 'dark') {
                            document.documentElement.classList.add('dark');
                        } else {
                            document.documentElement.classList.remove('dark');
                        }
                    }
                });
            ");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set up system theme change listener");
        }
    }
}
