using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using R2.ShopNet.Gateway.API.Configuration;

namespace R2.ShopNet.Gateway.API.Services;

/// <summary>
/// Background service that registers the gateway with Consul and deregisters on shutdown
/// </summary>
public sealed class ConsulRegistrationService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;
    private readonly IOptionsMonitor<GatewayOptions> _gatewayOptions;
    private readonly ILogger<ConsulRegistrationService> _logger;
    private string? _serviceId;

    public ConsulRegistrationService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ConsulOptions> consulOptions,
        IOptionsMonitor<GatewayOptions> gatewayOptions,
        ILogger<ConsulRegistrationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _consulOptions = consulOptions;
        _gatewayOptions = gatewayOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var gatewayConfig = _gatewayOptions.CurrentValue;
            var consulAddress = _consulOptions.CurrentValue.Address;
            
            _serviceId = $"{gatewayConfig.ServiceName}-{Environment.MachineName}-{Guid.NewGuid():N}";
            
            var registration = new
            {
                ID = _serviceId,
                Name = gatewayConfig.ServiceName,
                Address = "localhost",
                Port = gatewayConfig.HttpsPort,
                Tags = new[] { "api-gateway", "yarp", "v1" },
                Check = new
                {
                    HTTP = $"https://localhost:{gatewayConfig.HttpsPort}{gatewayConfig.HealthCheckPath}",
                    Interval = "30s",
                    Timeout = "10s",
                    DeregisterCriticalServiceAfter = "90s"
                }
            };

            using var client = _httpClientFactory.CreateClient("consul");
            var json = JsonSerializer.Serialize(registration, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"{consulAddress}/v1/agent/service/register";
            _logger.LogInformation("Registering gateway with Consul: {ServiceId} at {Url}", _serviceId, url);
            
            var response = await client.PutAsync(url, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered gateway with Consul: {ServiceId}", _serviceId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to register with Consul. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register gateway with Consul");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceId is null)
        {
            _logger.LogWarning("Service ID is null, skipping deregistration");
            return;
        }

        try
        {
            var consulAddress = _consulOptions.CurrentValue.Address;
            using var client = _httpClientFactory.CreateClient("consul");
            
            var url = $"{consulAddress}/v1/agent/service/deregister/{_serviceId}";
            _logger.LogInformation("Deregistering gateway from Consul: {ServiceId}", _serviceId);
            
            var response = await client.PutAsync(url, null, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deregistered gateway from Consul: {ServiceId}", _serviceId);
            }
            else
            {
                _logger.LogWarning("Failed to deregister from Consul. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deregistering gateway from Consul");
        }
    }
}
