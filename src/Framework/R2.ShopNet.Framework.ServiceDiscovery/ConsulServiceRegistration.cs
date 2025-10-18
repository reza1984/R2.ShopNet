using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace R2.ShopNet.Framework.ServiceDiscovery;

/// <summary>
/// Background service that registers and deregisters the service with Consul.
/// </summary>
public class ConsulServiceRegistration : IHostedService
{
    private readonly IConsulClient _consulClient;
    private readonly ConsulOptions _options;
    private string? _registrationId;

    public ConsulServiceRegistration(IConsulClient consulClient, IOptions<ConsulOptions> options)
    {
        _consulClient = consulClient;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _registrationId = string.IsNullOrEmpty(_options.ServiceId)
            ? $"{_options.ServiceName}-{Guid.NewGuid()}"
            : _options.ServiceId;

        var registration = new AgentServiceRegistration
        {
            ID = _registrationId,
            Name = _options.ServiceName,
            Address = _options.ServiceAddress,
            Port = _options.ServicePort,
            Tags = _options.Tags,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{_options.ServiceAddress}:{_options.ServicePort}/health",
                Interval = TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds),
                Timeout = TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(_options.DeregisterCriticalServiceAfterMinutes)
            }
        };

        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_registrationId))
        {
            await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
        }
    }
}
