using R2.ShopNet.Web.Portal.Models.Navigation;
using R2.ShopNet.Web.Portal.Services;
using R2.ShopNet.Web.Portal.Services.JsInterop;

namespace R2.ShopNet.Web.Portal.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds portal services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddPortalServices(this IServiceCollection services)
    {
        // UI state services
        services.AddScoped<ISidebarService, SidebarService>();
        services.AddScoped<IThemeService, ThemeService>();

        // JavaScript interop services
        services.AddScoped<IThemeJsInterop, ThemeJsInterop>();
        services.AddScoped<IWindowJsInterop, WindowJsInterop>();

        // Navigation configuration
        services.AddSingleton(NavigationConfiguration.GetDefault());

        return services;
    }
}
