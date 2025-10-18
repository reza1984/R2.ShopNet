using Consul;

namespace R2.ShopNet.Framework.ServiceDiscovery;

/// <summary>
/// Consul implementation of service discovery.
/// </summary>
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;

    public ConsulServiceDiscovery(IConsulClient consulClient)
    {
        _consulClient = consulClient;
    }

    public async Task<string?> GetServiceAddressAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await GetServiceAddressesAsync(serviceName, cancellationToken);
        return services.FirstOrDefault();
    }

    public async Task<IEnumerable<string>> GetServiceAddressesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Health.Service(serviceName, tag: null, passingOnly: true, cancellationToken);

        return services.Response.Select(s =>
        {
            var address = !string.IsNullOrEmpty(s.Service.Address)
                ? s.Service.Address
                : s.Node.Address;
            return $"{address}:{s.Service.Port}";
        });
    }
}
