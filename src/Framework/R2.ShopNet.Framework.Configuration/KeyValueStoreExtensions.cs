using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using R2.ShopNet.Framework.Configuration.Providers;
using StackExchange.Redis;

namespace R2.ShopNet.Framework.Configuration;

/// <summary>
/// Extension methods for registering key-value store services
/// </summary>
public static class KeyValueStoreExtensions
{
    /// <summary>
    /// Add Consul key-value store
    /// </summary>
    public static IServiceCollection AddConsulKeyValueStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConsulKeyValueOptions>(configuration.GetSection("Consul:KeyValue"));

        services.AddSingleton<IConsulClient>(sp =>
        {
            var options = configuration.GetSection("Consul:KeyValue").Get<ConsulKeyValueOptions>()
                ?? new ConsulKeyValueOptions();

            var consulConfig = new ConsulClientConfiguration
            {
                Address = new Uri(options.Address)
            };

            if (!string.IsNullOrEmpty(options.Token))
            {
                consulConfig.Token = options.Token;
            }

            if (!string.IsNullOrEmpty(options.Datacenter))
            {
                consulConfig.Datacenter = options.Datacenter;
            }

            return new ConsulClient(consulConfig);
        });

        services.AddSingleton<IKeyValueStore, ConsulKeyValueStore>();

        return services;
    }

    /// <summary>
    /// Add Consul key-value store with options
    /// </summary>
    public static IServiceCollection AddConsulKeyValueStore(
        this IServiceCollection services,
        Action<ConsulKeyValueOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddSingleton<IConsulClient>(sp =>
        {
            var options = new ConsulKeyValueOptions();
            configureOptions(options);

            var consulConfig = new ConsulClientConfiguration
            {
                Address = new Uri(options.Address)
            };

            if (!string.IsNullOrEmpty(options.Token))
            {
                consulConfig.Token = options.Token;
            }

            if (!string.IsNullOrEmpty(options.Datacenter))
            {
                consulConfig.Datacenter = options.Datacenter;
            }

            return new ConsulClient(consulConfig);
        });

        services.AddSingleton<IKeyValueStore, ConsulKeyValueStore>();

        return services;
    }

    /// <summary>
    /// Add Redis key-value store
    /// </summary>
    public static IServiceCollection AddRedisKeyValueStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisKeyValueOptions>(configuration.GetSection("Redis:KeyValue"));

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = configuration.GetSection("Redis:KeyValue").Get<RedisKeyValueOptions>()
                ?? new RedisKeyValueOptions();

            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.ConnectTimeout = options.ConnectTimeout;
            configOptions.SyncTimeout = options.SyncTimeout;
            configOptions.Ssl = options.UseSsl;

            return ConnectionMultiplexer.Connect(configOptions);
        });

        services.AddSingleton<IKeyValueStore, RedisKeyValueStore>();

        return services;
    }

    /// <summary>
    /// Add Redis key-value store with options
    /// </summary>
    public static IServiceCollection AddRedisKeyValueStore(
        this IServiceCollection services,
        Action<RedisKeyValueOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = new RedisKeyValueOptions();
            configureOptions(options);

            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.ConnectTimeout = options.ConnectTimeout;
            configOptions.SyncTimeout = options.SyncTimeout;
            configOptions.Ssl = options.UseSsl;

            return ConnectionMultiplexer.Connect(configOptions);
        });

        services.AddSingleton<IKeyValueStore, RedisKeyValueStore>();

        return services;
    }

    /// <summary>
    /// Add ConfigurationManager service for high-level configuration management
    /// </summary>
    public static IServiceCollection AddConfigurationManager(this IServiceCollection services)
    {
        services.AddSingleton<ConfigurationManager>();
        return services;
    }

    /// <summary>
    /// Add Azure Key Vault store
    /// </summary>
    public static IServiceCollection AddAzureKeyVaultStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureKeyVaultOptions>(configuration.GetSection("AzureKeyVault"));
        services.AddSingleton<IKeyValueStore, AzureKeyVaultStore>();
        return services;
    }

    /// <summary>
    /// Add Azure Key Vault store with options
    /// </summary>
    public static IServiceCollection AddAzureKeyVaultStore(
        this IServiceCollection services,
        Action<AzureKeyVaultOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IKeyValueStore, AzureKeyVaultStore>();
        return services;
    }

    /// <summary>
    /// Add AWS Secrets Manager store
    /// </summary>
    public static IServiceCollection AddAwsSecretsManagerStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AwsSecretsManagerOptions>(configuration.GetSection("AwsSecretsManager"));
        services.AddSingleton<IKeyValueStore, AwsSecretsManagerStore>();
        return services;
    }

    /// <summary>
    /// Add AWS Secrets Manager store with options
    /// </summary>
    public static IServiceCollection AddAwsSecretsManagerStore(
        this IServiceCollection services,
        Action<AwsSecretsManagerOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IKeyValueStore, AwsSecretsManagerStore>();
        return services;
    }

    /// <summary>
    /// Add key-value store based on configuration provider setting
    /// </summary>
    public static IServiceCollection AddKeyValueStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("KeyValueStore").Get<KeyValueStoreOptions>();
        
        if (options == null)
        {
            throw new InvalidOperationException("KeyValueStore configuration section not found.");
        }

        return options.Provider.ToLowerInvariant() switch
        {
            "consul" => services.AddConsulKeyValueStore(configuration),
            "redis" => services.AddRedisKeyValueStore(configuration),
            "azurekeyvault" => services.AddAzureKeyVaultStore(configuration),
            "awssecretsmanager" => services.AddAwsSecretsManagerStore(configuration),
            _ => throw new InvalidOperationException($"Unknown key-value store provider: {options.Provider}")
        };
    }
}
