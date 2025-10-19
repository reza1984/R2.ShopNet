using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace R2.ShopNet.Framework.Configuration.Integration;

/// <summary>
/// Hosted service that initializes key-value configuration providers on application startup.
/// - Seeds configuration from appsettings.json if it doesn't exist in the key-value store
/// - Initializes all key-value configuration providers with the IKeyValueStore instance
/// </summary>
public class KeyValueConfigurationInitializer : IHostedService
{
    private readonly IKeyValueStore _kvStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyValueConfigurationInitializer> _logger;
    private readonly IEnumerable<KeyValueConfigurationProvider> _kvProviders;

    public KeyValueConfigurationInitializer(
        IKeyValueStore kvStore,
        IConfiguration configuration,
        ILogger<KeyValueConfigurationInitializer> logger,
        IEnumerable<KeyValueConfigurationProvider> kvProviders)
    {
        _kvStore = kvStore;
        _configuration = configuration;
        _logger = logger;
        _kvProviders = kvProviders;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing key-value configuration providers");

        try
        {
            // Seed configuration if it doesn't exist
            await SeedConfigurationAsync(cancellationToken);

            // Initialize all providers with the key-value store
            foreach (var provider in _kvProviders)
            {
                provider.SetKeyValueStore(_kvStore);
                provider.Load();
            }

            _logger.LogInformation("Key-value configuration providers initialized successfully");

            // Log loaded configuration (without sensitive values)
            LogConfigurationState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize key-value configuration. Using fallback configuration only");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds configuration from appsettings.json to the key-value store if it doesn't exist
    /// Override this method in derived classes to customize seeding logic
    /// </summary>
    protected virtual async Task SeedConfigurationAsync(CancellationToken cancellationToken)
    {
        // Default implementation: do nothing
        // Services can override this to seed their specific configuration
        await Task.CompletedTask;
    }

    /// <summary>
    /// Logs the current configuration state (useful for debugging)
    /// </summary>
    private void LogConfigurationState()
    {
        try
        {
            // Log some common configuration keys (customize per service)
            var configKeys = new[] { "Jwt:Issuer", "Jwt:Audience", "Database:Provider" };

            foreach (var key in configKeys)
            {
                var value = _configuration[key];
                if (!string.IsNullOrEmpty(value))
                {
                    _logger.LogDebug("Configuration loaded - {Key}: {Value}", key, value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to log configuration state");
        }
    }
}
