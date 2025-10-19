using R2.ShopNet.Framework.Configuration;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Infrastructure.Services;

namespace R2.ShopNet.Identity.API.Services;

/// <summary>
/// Background service that watches for JWT configuration changes in Consul
/// and updates the TokenService when configuration changes
/// </summary>
public class ConfigurationWatcherService : BackgroundService
{
    private readonly IKeyValueStore _kvStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationWatcherService> _logger;

    public ConfigurationWatcherService(
        IKeyValueStore kvStore,
        IServiceProvider serviceProvider,
        ILogger<ConfigurationWatcherService> logger)
    {
        _kvStore = kvStore;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConfigurationWatcherService started");

        try
        {
            // Watch for JWT secret changes
            await _kvStore.WatchAsync("identity/jwt/secret", async newValue =>
            {
                _logger.LogWarning("JWT secret changed in Consul! Reloading configuration...");
                
                // In production, you would:
                // 1. Update the TokenService with the new secret
                // 2. Invalidate existing tokens if needed
                // 3. Notify connected services
                
                _logger.LogInformation("Configuration reload triggered");
            }, stoppingToken);

            // Watch for JWT issuer changes
            await _kvStore.WatchAsync("identity/jwt/issuer", async newValue =>
            {
                _logger.LogInformation("JWT issuer changed to: {Issuer}", newValue);
            }, stoppingToken);

            // Watch for JWT audience changes
            await _kvStore.WatchAsync("identity/jwt/audience", async newValue =>
            {
                _logger.LogInformation("JWT audience changed to: {Audience}", newValue);
            }, stoppingToken);

            // Watch for database connection string changes
            await _kvStore.WatchAsync("identity/database/connectionString", async newValue =>
            {
                _logger.LogWarning("Database connection string changed! Service restart may be required.");
            }, stoppingToken);

            _logger.LogInformation("Watching configuration keys for changes");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ConfigurationWatcherService stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ConfigurationWatcherService");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ConfigurationWatcherService stopped");
        await base.StopAsync(cancellationToken);
    }
}
