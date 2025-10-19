using System.Text.Json.Serialization;

namespace R2.ShopNet.Gateway.API.Models;

/// <summary>
/// Represents a health check response from Consul
/// </summary>
public sealed class ConsulHealthCheck
{
    [JsonPropertyName("Node")]
    public ConsulNode Node { get; set; } = null!;

    [JsonPropertyName("Service")]
    public ConsulService Service { get; set; } = null!;

    [JsonPropertyName("Checks")]
    public List<ConsulCheck> Checks { get; set; } = new();
}

public sealed class ConsulNode
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Node")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public string Address { get; set; } = string.Empty;
}

public sealed class ConsulService
{
    [JsonPropertyName("ID")]
    public string ID { get; set; } = string.Empty;

    [JsonPropertyName("Service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("Port")]
    public int Port { get; set; }

    [JsonPropertyName("Tags")]
    public List<string> Tags { get; set; } = new();
}

public sealed class ConsulCheck
{
    [JsonPropertyName("CheckID")]
    public string CheckId { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("Output")]
    public string Output { get; set; } = string.Empty;
}

/// <summary>
/// Represents a service instance discovered from Consul
/// </summary>
public sealed class ServiceInstance
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> Tags { get; set; } = new();
}
