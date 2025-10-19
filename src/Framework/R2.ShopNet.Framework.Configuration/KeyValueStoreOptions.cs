namespace R2.ShopNet.Framework.Configuration;

/// <summary>
/// Options for key-value store providers
/// </summary>
public class KeyValueStoreOptions
{
    /// <summary>
    /// Provider type (Consul, Redis, AzureKeyVault, AwsSecretsManager)
    /// </summary>
    public string Provider { get; set; } = "Consul";

    /// <summary>
    /// Configuration specific to the provider
    /// </summary>
    public IDictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Options for Consul KV store
/// </summary>
public class ConsulKeyValueOptions
{
    public string Address { get; set; } = "http://localhost:8500";
    public string? Token { get; set; }
    public string? Datacenter { get; set; }
    public string KeyPrefix { get; set; } = "";
    
    /// <summary>
    /// Maximum number of concurrent connections to Consul
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 5;
    
    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 10;
    
    /// <summary>
    /// Base delay in seconds for exponential backoff
    /// </summary>
    public int BaseDelaySeconds { get; set; } = 5;
    
    /// <summary>
    /// Maximum delay in seconds for exponential backoff
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 300;
}

/// <summary>
/// Options for Redis KV store
/// </summary>
public class RedisKeyValueOptions
{
    /// <summary>
    /// Redis connection string (e.g., "localhost:6379,password=mypassword")
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";
    
    /// <summary>
    /// Redis database number (0-15)
    /// </summary>
    public int Database { get; set; } = 0;
    
    /// <summary>
    /// Optional key prefix for multi-tenancy (e.g., "shopnet:identity-service:")
    /// </summary>
    public string KeyPrefix { get; set; } = "";
    
    /// <summary>
    /// Default expiration time in seconds (null = no expiration)
    /// </summary>
    public int? DefaultExpiration { get; set; }
    
    /// <summary>
    /// Enable SSL/TLS connection
    /// </summary>
    public bool UseSsl { get; set; } = false;
    
    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;
    
    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;
}

/// <summary>
/// Options for Azure Key Vault
/// </summary>
public class AzureKeyVaultOptions
{
    public string VaultUri { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
}

/// <summary>
/// Options for AWS Secrets Manager
/// </summary>
public class AwsSecretsManagerOptions
{
    public string Region { get; set; } = "us-east-1";
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? ProfileName { get; set; }
    public bool UseInstanceProfile { get; set; } = true;
    public string KeyPrefix { get; set; } = "";
}
