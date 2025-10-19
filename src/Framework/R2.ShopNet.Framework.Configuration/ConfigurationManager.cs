using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace R2.ShopNet.Framework.Configuration;

/// <summary>
/// Service for managing application configuration in key-value stores
/// </summary>
public class ConfigurationManager
{
    private readonly IKeyValueStore _keyValueStore;
    private readonly ILogger<ConfigurationManager> _logger;

    public ConfigurationManager(
        IKeyValueStore keyValueStore,
        ILogger<ConfigurationManager> logger)
    {
        _keyValueStore = keyValueStore;
        _logger = logger;
    }

    /// <summary>
    /// Get a configuration value and deserialize to type T
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _keyValueStore.GetAsync(key, cancellationToken);
            
            if (value == null)
            {
                return default;
            }

            // Try to deserialize as JSON first
            if (typeof(T) != typeof(string) && (value.StartsWith('{') || value.StartsWith('[')))
            {
                return JsonSerializer.Deserialize<T>(value);
            }

            // For simple types, convert directly
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Set a configuration value (will serialize complex types to JSON)
    /// </summary>
    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            string stringValue;

            if (value is string str)
            {
                stringValue = str;
            }
            else if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            else
            {
                // Serialize complex types to JSON
                stringValue = JsonSerializer.Serialize(value);
            }

            await _keyValueStore.SetAsync(key, stringValue, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration value: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Get all configuration values under a prefix
    /// </summary>
    public async Task<IDictionary<string, string>> GetSectionAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _keyValueStore.GetByPrefixAsync(prefix, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration section: {Prefix}", prefix);
            throw;
        }
    }

    /// <summary>
    /// Set multiple configuration values
    /// </summary>
    public async Task SetSectionAsync(
        IDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _keyValueStore.SetManyAsync(values, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration section");
            throw;
        }
    }

    /// <summary>
    /// Delete a configuration value
    /// </summary>
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _keyValueStore.DeleteAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration value: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Delete all configuration values under a prefix
    /// </summary>
    public async Task DeleteSectionAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            await _keyValueStore.DeleteByPrefixAsync(prefix, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration section: {Prefix}", prefix);
            throw;
        }
    }

    /// <summary>
    /// Check if a configuration key exists
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _keyValueStore.ExistsAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking configuration key existence: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Watch for changes to a configuration key
    /// </summary>
    public async Task WatchAsync<T>(
        string key,
        Action<T?> onChanged,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _keyValueStore.WatchAsync(key, value =>
            {
                if (value == null)
                {
                    onChanged(default);
                    return;
                }

                try
                {
                    T? typedValue;

                    // Try to deserialize as JSON first
                    if (typeof(T) != typeof(string) && (value.StartsWith('{') || value.StartsWith('[')))
                    {
                        typedValue = JsonSerializer.Deserialize<T>(value);
                    }
                    else
                    {
                        // For simple types, convert directly
                        typedValue = (T)Convert.ChangeType(value, typeof(T));
                    }

                    onChanged(typedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing watched configuration value: {Key}", key);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching configuration key: {Key}", key);
            throw;
        }
    }
}
