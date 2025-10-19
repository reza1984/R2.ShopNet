using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using R2.ShopNet.Gateway.API.Configuration;

namespace R2.ShopNet.Gateway.API.HealthChecks;

/// <summary>
/// Health check that verifies Consul is reachable
/// </summary>
public sealed class ConsulHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;
    private readonly ILogger<ConsulHealthCheck> _logger;

    public ConsulHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ConsulOptions> consulOptions,
        ILogger<ConsulHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _consulOptions = consulOptions;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("consul");
            var consulAddress = _consulOptions.CurrentValue.Address;
            
            _logger.LogDebug("Checking Consul health at {ConsulAddress}", consulAddress);
            
            var response = await client.GetAsync(
                $"{consulAddress}/v1/status/leader",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var leader = await response.Content.ReadAsStringAsync(cancellationToken);
                return HealthCheckResult.Healthy($"Consul is reachable. Leader: {leader}");
            }

            return HealthCheckResult.Unhealthy($"Consul returned status code: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Consul");
            return HealthCheckResult.Unhealthy("Cannot reach Consul", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking Consul health");
            return HealthCheckResult.Unhealthy("Unexpected error checking Consul", ex);
        }
    }
}
