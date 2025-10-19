using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace R2.ShopNet.Framework.Configuration.Integration;

/// <summary>
/// Configuration provider that loads configuration from a key-value store (Consul, Redis, etc.)
/// and integrates with .NET's IConfiguration system
/// </summary>
public class KeyValueConfigurationProvider : ConfigurationProvider
{
    private readonly KeyValueConfigurationSource _source;
    private IKeyValueStore? _kvStore;

    public KeyValueConfigurationProvider(KeyValueConfigurationSource source)
    {
        _source = source;
    }

    /// <summary>
    /// Sets the key-value store instance (called after DI is configured)
    /// </summary>
    public void SetKeyValueStore(IKeyValueStore kvStore)
    {
        _kvStore = kvStore;
    }

    /// <summary>
    /// Loads configuration from the key-value store
    /// </summary>
    public override void Load()
    {
        if (_kvStore == null)
        {
            return; // Will be initialized later by the hosted service
        }

        LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        if (_kvStore == null)
        {
            return;
        }

        try
        {
            // Load all keys with the specified prefix
            var kvPairs = await _kvStore.GetByPrefixAsync(_source.KeyPrefix);

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in kvPairs)
            {
                // Convert key from "prefix/section/key" to "Section:Key" format
                var configKey = ConvertKey(kvp.Key);
                data[configKey] = kvp.Value;
            }

            Data = data;

            // Start watching for changes (hot-reload)
            StartWatching();
        }
        catch (Exception)
        {
            // If loading fails, keep empty configuration
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Converts key-value store key format to .NET configuration key format
    /// Examples:
    /// - "identity/jwt/secret" -> "Jwt:Secret"
    /// - "catalog/database/connectionString" -> "Database:ConnectionString"
    /// </summary>
    private string ConvertKey(string key)
    {
        // Remove the prefix
        if (key.StartsWith(_source.KeyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            key = key.Substring(_source.KeyPrefix.Length);
        }

        // Remove leading slashes
        key = key.TrimStart('/');

        // Split by '/' and capitalize each segment, then join with ':'
        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var capitalizedSegments = segments.Select(s =>
            char.ToUpperInvariant(s[0]) + s.Substring(1));

        return string.Join(":", capitalizedSegments);
    }

    /// <summary>
    /// Starts watching for configuration changes and triggers reload
    /// </summary>
    private void StartWatching()
    {
        if (_kvStore == null)
        {
            return;
        }

        // Watch for changes to keys with this prefix
        // The callback is called when any key with the prefix changes
        _ = _kvStore.WatchAsync(_source.KeyPrefix, async (changedKey) =>
        {
            await LoadAsync();
            OnReload(); // Notify configuration system that data has changed
        });
    }
}
