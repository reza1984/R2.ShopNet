using Microsoft.JSInterop;

namespace R2.ShopNet.Web.Portal.Services.JsInterop;

/// <summary>
/// JavaScript interop service for window-related operations
/// </summary>
public class WindowJsInterop : IWindowJsInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<WindowJsInterop> _logger;

    public WindowJsInterop(IJSRuntime jsRuntime, ILogger<WindowJsInterop> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<int> GetWindowWidthAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<int>("eval", "window.innerWidth");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get window width");
            return 0;
        }
    }
}
