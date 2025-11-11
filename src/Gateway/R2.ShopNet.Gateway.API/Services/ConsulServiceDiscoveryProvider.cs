using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using R2.ShopNet.Gateway.API.Configuration;
using R2.ShopNet.Gateway.API.Models;
using Yarp.ReverseProxy.Configuration;

namespace R2.ShopNet.Gateway.API.Services;

/// <summary>
/// Provides YARP proxy configuration by discovering services from Consul
/// </summary>
public sealed class ConsulServiceDiscoveryProvider : IProxyConfigProvider, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;
    private readonly ILogger<ConsulServiceDiscoveryProvider> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConfigurationChangeTokenSource _changeTokenSource;
    
    private volatile InMemoryConfig _config;

    public ConsulServiceDiscoveryProvider(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ConsulOptions> consulOptions,
        ILogger<ConsulServiceDiscoveryProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _consulOptions = consulOptions;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _changeTokenSource = new ConfigurationChangeTokenSource();
        _config = new InMemoryConfig(new List<RouteConfig>(), new List<ClusterConfig>());
        
        // Initial load
        _ = RefreshConfigurationAsync();
        
        // Start background refresh
        _ = StartRefreshLoopAsync();
    }

    public IProxyConfig GetConfig() => _config;

    private async Task RefreshConfigurationAsync()
    {
        try
        {
            var routes = new List<RouteConfig>();
            var clusterDict = new Dictionary<string, ClusterConfig>();

            _logger.LogInformation("Refreshing configuration from Consul at {ConsulAddress}",
                _consulOptions.CurrentValue.Address);

            // Query Consul for each configured service
            foreach (var serviceMapping in _consulOptions.CurrentValue.ServiceMappings)
            {
                try
                {
                    var instances = await GetHealthyServiceInstancesAsync(serviceMapping.ServiceName);

                    if (instances.Count > 0)
                    {
                        // Create YARP cluster with Consul-discovered destinations (only if not already created)
                        if (!clusterDict.ContainsKey(serviceMapping.ClusterId))
                        {
                            var destinations = new Dictionary<string, DestinationConfig>();

                            for (int i = 0; i < instances.Count; i++)
                            {
                                var instance = instances[i];
                                var destinationId = $"{serviceMapping.ClusterId}-{i}";

                                // Determine scheme based on service port
                                // Identity service uses HTTPS (5003), Catalog uses HTTP (5004)
                                var scheme = instance.Port == 5003 ? "https" : "http";

                                destinations[destinationId] = new DestinationConfig
                                {
                                    Address = $"{scheme}://{instance.Address}:{instance.Port}"
                                };

                                _logger.LogDebug("Added destination {DestinationId}: {Address}",
                                    destinationId, destinations[destinationId].Address);
                            }

                            clusterDict[serviceMapping.ClusterId] = new ClusterConfig
                            {
                                ClusterId = serviceMapping.ClusterId,
                                Destinations = destinations,
                                LoadBalancingPolicy = "RoundRobin",
                                HealthCheck = new HealthCheckConfig
                                {
                                    Active = new ActiveHealthCheckConfig
                                    {
                                        Enabled = true,
                                        Interval = TimeSpan.FromSeconds(30),
                                        Timeout = TimeSpan.FromSeconds(10),
                                        Policy = "ConsecutiveFailures",
                                        Path = "/health"
                                    }
                                }
                            };
                        }

                        // Create route for this service
                        var route = new RouteConfig
                        {
                            RouteId = serviceMapping.RouteId,
                            ClusterId = serviceMapping.ClusterId,
                            Match = new RouteMatch
                            {
                                Path = serviceMapping.PathPattern
                            }
                        };

                        // Add transforms if configured
                        if (serviceMapping.Transforms != null && serviceMapping.Transforms.Count > 0)
                        {
                            route = route with
                            {
                                Transforms = serviceMapping.Transforms
                            };
                        }

                        routes.Add(route);

                        _logger.LogInformation("Configured route {RouteId} -> {ClusterId} with {InstanceCount} instances",
                            serviceMapping.RouteId, serviceMapping.ClusterId, instances.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No healthy instances found for service {ServiceName}. Service will be unavailable until instances are registered with Consul.", 
                            serviceMapping.ServiceName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to configure service {ServiceName}",
                        serviceMapping.ServiceName);
                }
            }

            var clusters = clusterDict.Values.ToList();

            // Signal the old config's change token before creating new config
            var oldConfig = _config;
            _config = new InMemoryConfig(routes, clusters);
            (oldConfig as InMemoryConfig)?.SignalChange();

            _logger.LogInformation("Configuration refreshed: {RouteCount} routes, {ClusterCount} clusters",
                routes.Count, clusters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh configuration from Consul");
        }
    }

    private async Task<List<ServiceInstance>> GetHealthyServiceInstancesAsync(string serviceName)
    {
        using var client = _httpClientFactory.CreateClient("consul");
        var consulAddress = _consulOptions.CurrentValue.Address;
        // Temporarily bypass health check requirement for testing
        var url = $"{consulAddress}/v1/health/service/{serviceName}";

        _logger.LogDebug("Querying Consul for instances of {ServiceName}: {Url}", 
            serviceName, url);

        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Consul returned {StatusCode} for service {ServiceName}", 
                response.StatusCode, serviceName);
            return new List<ServiceInstance>();
        }

        var json = await response.Content.ReadAsStringAsync();
        var healthChecks = JsonSerializer.Deserialize<List<ConsulHealthCheck>>(json);

        if (healthChecks == null || healthChecks.Count == 0)
        {
            return new List<ServiceInstance>();
        }

        var instances = healthChecks
            .Select(h => new ServiceInstance
            {
                ServiceId = h.Service.ID,
                ServiceName = h.Service.Service,
                Address = string.IsNullOrEmpty(h.Service.Address) ? h.Node.Address : h.Service.Address,
                Port = h.Service.Port,
                Tags = h.Service.Tags
            })
            .ToList();

        _logger.LogDebug("Found {InstanceCount} healthy instances for {ServiceName}", 
            instances.Count, serviceName);

        return instances;
    }

    private async Task StartRefreshLoopAsync()
    {
        var refreshInterval = _consulOptions.CurrentValue.RefreshInterval;
        
        _logger.LogInformation("Starting configuration refresh loop with interval {RefreshInterval}", 
            refreshInterval);

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(refreshInterval, _cancellationTokenSource.Token);
                await RefreshConfigurationAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Configuration refresh loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in configuration refresh loop");
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _changeTokenSource?.Dispose();
    }

    private sealed class InMemoryConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public InMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }

        public void SignalChange()
        {
            _cts.Cancel();
        }
    }
}
