namespace R2.ShopNet.Framework.Configuration;

/// <summary>
/// Interface for key-value configuration stores (Consul, Azure Key Vault, AWS Secrets Manager, etc.)
/// </summary>
public interface IKeyValueStore : IDisposable
{
    /// <summary>
    /// Get a configuration value by key
    /// </summary>
    /// <param name="key">Configuration key (e.g., "database/connectionstring")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration value or null if not found</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple configuration values by keys
    /// </summary>
    /// <param name="keys">List of configuration keys</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of key-value pairs</returns>
    Task<IDictionary<string, string>> GetManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configuration values under a key prefix
    /// </summary>
    /// <param name="prefix">Key prefix (e.g., "identity-service/")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of key-value pairs</returns>
    Task<IDictionary<string, string>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a configuration value
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set multiple configuration values
    /// </summary>
    /// <param name="values">Dictionary of key-value pairs to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetManyAsync(IDictionary<string, string> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a configuration value
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all configuration values under a key prefix
    /// </summary>
    /// <param name="prefix">Key prefix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Watch for changes to a configuration key
    /// </summary>
    /// <param name="key">Configuration key to watch</param>
    /// <param name="onChanged">Callback when value changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WatchAsync(string key, Action<string?> onChanged, CancellationToken cancellationToken = default);
}
