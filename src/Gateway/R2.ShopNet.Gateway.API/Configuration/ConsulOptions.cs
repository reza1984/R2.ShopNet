namespace R2.ShopNet.Gateway.API.Configuration;

/// <summary>
/// Configuration options for Consul service discovery integration
/// </summary>
public sealed class ConsulOptions
{
    public const string SectionName = "Consul";

    /// <summary>
    /// Consul HTTP API address (e.g., http://localhost:8500)
    /// </summary>
    public string Address { get; set; } = "http://localhost:8500";

    /// <summary>
    /// Mappings from route patterns to Consul service names
    /// </summary>
    public List<ServiceMapping> ServiceMappings { get; set; } = new();

    /// <summary>
    /// Interval for refreshing service discovery configuration
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Maps a route to a Consul service
/// </summary>
public sealed class ServiceMapping
{
    /// <summary>
    /// Unique identifier for the route (e.g., "identity-route")
    /// </summary>
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the YARP cluster (e.g., "identity-cluster")
    /// </summary>
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the service in Consul (e.g., "identity-service")
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Path pattern to match requests (e.g., "/api/identity/{**catch-all}")
    /// </summary>
    public string PathPattern { get; set; } = string.Empty;

    /// <summary>
    /// Optional path transformations to apply before forwarding
    /// </summary>
    public List<Dictionary<string, string>>? Transforms { get; set; }
}
