namespace R2.ShopNet.Framework.ServiceDiscovery;

/// <summary>
/// Configuration options for Consul service discovery.
/// </summary>
public class ConsulOptions
{
    public string Address { get; set; } = "http://localhost:8500";
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = "localhost";
    public int ServicePort { get; set; }
    public string HealthCheckUrl { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int HealthCheckIntervalSeconds { get; set; } = 10;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
    public int DeregisterCriticalServiceAfterMinutes { get; set; } = 1;
}
