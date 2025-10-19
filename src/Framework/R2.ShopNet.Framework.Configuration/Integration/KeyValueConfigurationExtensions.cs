using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Framework.Configuration.Integration;

/// <summary>
/// Extension methods for integrating key-value stores with .NET Configuration system
/// </summary>
public static class KeyValueConfigurationExtensions
{
    /// <summary>
    /// Adds a key-value store (Consul, Redis, etc.) as a configuration source
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="keyPrefix">Key prefix to use (e.g., "identity/", "catalog/")</param>
    /// <returns>Configuration builder for chaining</returns>
    public static IConfigurationBuilder AddKeyValueConfiguration(
        this IConfigurationBuilder builder,
        string keyPrefix)
    {
        var source = new KeyValueConfigurationSource
        {
            KeyPrefix = keyPrefix
        };

        builder.Add(source);
        return builder;
    }

    /// <summary>
    /// Registers key-value configuration services in DI
    /// This should be called after AddKeyValueConfiguration to register providers
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration (must be IConfigurationRoot)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddKeyValueConfigurationServices(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        // Find and register all key-value configuration providers
        foreach (var provider in configuration.Providers.OfType<KeyValueConfigurationProvider>())
        {
            services.AddSingleton(provider);
        }

        return services;
    }

    /// <summary>
    /// Registers the key-value configuration initializer as a hosted service
    /// </summary>
    /// <typeparam name="TInitializer">Type of initializer (must inherit from KeyValueConfigurationInitializer)</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddKeyValueConfigurationInitializer<TInitializer>(
        this IServiceCollection services)
        where TInitializer : KeyValueConfigurationInitializer
    {
        services.AddHostedService<TInitializer>();
        return services;
    }

    /// <summary>
    /// Registers the default key-value configuration initializer as a hosted service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddKeyValueConfigurationInitializer(
        this IServiceCollection services)
    {
        services.AddHostedService<KeyValueConfigurationInitializer>();
        return services;
    }
}
