namespace R2.ShopNet.Web.Portal.Services.JsInterop;

/// <summary>
/// JavaScript interop service for window-related operations
/// </summary>
public interface IWindowJsInterop
{
    /// <summary>
    /// Gets the current window width in pixels
    /// </summary>
    Task<int> GetWindowWidthAsync();
}
