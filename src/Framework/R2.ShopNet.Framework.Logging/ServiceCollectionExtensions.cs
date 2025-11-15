using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace R2.ShopNet.Framework.Logging;

/// <summary>
/// Extension methods for configuring logging services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds R2.ShopNet logging configuration to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddR2ShopNetLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loggingOptions = new LoggingOptions();
        configuration.GetSection("Logging:R2ShopNet").Bind(loggingOptions);

        services.Configure<LoggingOptions>(
            configuration.GetSection("Logging:R2ShopNet"));

        return services;
    }

    /// <summary>
    /// Adds R2.ShopNet logging configuration with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure logging options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddR2ShopNetLogging(
        this IServiceCollection services,
        Action<LoggingOptions> configure)
    {
        services.Configure(configure);
        return services;
    }
}
