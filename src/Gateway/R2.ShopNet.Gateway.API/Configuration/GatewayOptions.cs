namespace R2.ShopNet.Gateway.API.Configuration;

/// <summary>
/// Configuration options for the API Gateway
/// </summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>
    /// Service name to register in Consul
    /// </summary>
    public string ServiceName { get; set; } = "gateway";

    /// <summary>
    /// Port the gateway listens on (HTTP)
    /// </summary>
    public int HttpPort { get; set; } = 5000;

    /// <summary>
    /// Port the gateway listens on (HTTPS)
    /// </summary>
    public int HttpsPort { get; set; } = 5003;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Readiness check endpoint path
    /// </summary>
    public string ReadyCheckPath { get; set; } = "/ready";
}
