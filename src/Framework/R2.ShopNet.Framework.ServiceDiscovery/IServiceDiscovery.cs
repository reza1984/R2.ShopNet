namespace R2.ShopNet.Framework.ServiceDiscovery;

/// <summary>
/// Interface for service discovery operations.
/// </summary>
public interface IServiceDiscovery
{
    Task<string?> GetServiceAddressAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetServiceAddressesAsync(string serviceName, CancellationToken cancellationToken = default);
}
