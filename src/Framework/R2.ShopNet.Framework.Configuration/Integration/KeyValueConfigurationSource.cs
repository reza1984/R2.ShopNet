using Microsoft.Extensions.Configuration;

namespace R2.ShopNet.Framework.Configuration.Integration;

/// <summary>
/// Configuration source that loads configuration from a key-value store
/// </summary>
public class KeyValueConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Key prefix to use when loading configuration (e.g., "identity/", "catalog/")
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Reloadable token for hot-reload support
    /// </summary>
    public IConfiguration? Configuration { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new KeyValueConfigurationProvider(this);
    }
}
