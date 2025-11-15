using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using R2.ShopNet.Gateway.API.Configuration;

namespace R2.ShopNet.Gateway.API.HealthChecks;

/// <summary>
/// Health check that verifies Consul is reachable
/// </summary>
public sealed class ConsulHealthCheck : IHealthCheck, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;
    private readonly ILogger<ConsulHealthCheck> _logger;

    public ConsulHealthCheck(
        IOptionsMonitor<ConsulOptions> consulOptions,
        ILogger<ConsulHealthCheck> logger)
    {
        // Use a dedicated HttpClient to avoid service discovery circular dependency
        // Do NOT use IHttpClientFactory here as it applies service discovery by default
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _consulOptions = consulOptions;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var consulAddress = _consulOptions.CurrentValue.Address;

            _logger.LogDebug("Checking Consul health at {ConsulAddress}", consulAddress);

            var response = await _httpClient.GetAsync(
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

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
