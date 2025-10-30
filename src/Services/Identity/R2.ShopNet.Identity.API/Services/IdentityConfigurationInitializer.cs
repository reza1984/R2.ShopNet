using R2.ShopNet.Framework.Configuration;
using R2.ShopNet.Framework.Configuration.Integration;

namespace R2.ShopNet.Identity.API.Services;

/// <summary>
/// Identity service-specific configuration initializer.
/// Seeds JWT and database configuration to the key-value store on first run.
/// </summary>
public class IdentityConfigurationInitializer : KeyValueConfigurationInitializer
{
    private readonly IKeyValueStore _kvStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityConfigurationInitializer> _logger;

    public IdentityConfigurationInitializer(
        IKeyValueStore kvStore,
        IConfiguration configuration,
        ILogger<IdentityConfigurationInitializer> logger,
        IEnumerable<KeyValueConfigurationProvider> kvProviders)
        : base(kvStore, configuration, logger, kvProviders)
    {
        _kvStore = kvStore;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task SeedConfigurationAsync(CancellationToken cancellationToken)
    {
        // Check if JWT configuration exists in the key-value store, if not seed it
        var keyToCheck = "identity/jwt/secret";
        var exists = await _kvStore.ExistsAsync(keyToCheck, cancellationToken);
        
        _logger.LogInformation(
            "Configuration seeding check - Key: {Key}, Exists: {Exists}", 
            keyToCheck, 
            exists
        );
        
        if (!exists)
        {
            _logger.LogWarning("JWT configuration not found in Consul. This should only happen on FIRST RUN or after Consul data wipe.");
            _logger.LogInformation("Seeding Identity configuration to key-value store");

            // Seed JWT configuration
            await _kvStore.SetAsync("identity/jwt/secret",
                _configuration["Jwt:Secret"] ?? "your-256-bit-secret-key-here-change-in-production!!",
                cancellationToken);
            
            await _kvStore.SetAsync("identity/jwt/issuer",
                _configuration["Jwt:Issuer"] ?? "R2.ShopNet.Identity",
                cancellationToken);
            
            await _kvStore.SetAsync("identity/jwt/audience",
                _configuration["Jwt:Audience"] ?? "R2.ShopNet",
                cancellationToken);
            
            await _kvStore.SetAsync("identity/jwt/expirationMinutes",
                _configuration["Jwt:ExpirationMinutes"] ?? "60",
                cancellationToken);

            // Seed database connection string
            await _kvStore.SetAsync("identity/database/connectionString",
                _configuration.GetConnectionString("IdentityDb")
                ?? "Host=localhost;Port=5432;Database=identitydb;Username=postgres;Password=postgres",
                cancellationToken);

            // Seed Consul service discovery configuration
            await _kvStore.SetAsync("identity/consul/address",
                _configuration["Consul:Address"] ?? "http://localhost:8500",
                cancellationToken);

            await _kvStore.SetAsync("identity/consul/serviceName",
                _configuration["Consul:ServiceName"] ?? "identity-service",
                cancellationToken);

            await _kvStore.SetAsync("identity/consul/serviceId",
                _configuration["Consul:ServiceId"] ?? "identity-service-1",
                cancellationToken);

            await _kvStore.SetAsync("identity/consul/serviceAddress",
                _configuration["Consul:ServiceAddress"] ?? "http://localhost",
                cancellationToken);

            await _kvStore.SetAsync("identity/consul/servicePort",
                _configuration["Consul:ServicePort"] ?? "5002",
                cancellationToken);

            // Seed Redis configuration
            await _kvStore.SetAsync("identity/redis/connectionString",
                _configuration["Redis:KeyValue:ConnectionString"] ?? "localhost:6379",
                cancellationToken);

            await _kvStore.SetAsync("identity/redis/database",
                _configuration["Redis:KeyValue:Database"] ?? "0",
                cancellationToken);

            _logger.LogInformation("Identity configuration seeded successfully");
        }
        else
        {
            _logger.LogInformation("Identity configuration already exists in Consul. Skipping seed. (This is normal for subsequent runs)");
        }
    }
}

