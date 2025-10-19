# Design Document: API Gateway with Consul Integration

**Change ID:** `add-api-gateway-with-consul`  
**Last Updated:** 2025-10-19  

## Architecture Overview

### System Context

The API Gateway serves as a centralized entry point for all client applications, providing service discovery, routing, authentication, and cross-cutting concerns. It integrates with the existing Consul service registry to dynamically discover and route to backend microservices.

```
┌──────────────────────────────────────────────────────────────────┐
│                         External Clients                          │
└──────────────────────┬───────────────────────────────────────────┘
                       │ HTTPS
                       │
                ┌──────▼──────┐
                │   Gateway   │ ◄─── Service Discovery
                │    (YARP)   │      (Consul)
                └──────┬──────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
    ┌────────┐    ┌────────┐    ┌────────┐
    │Identity│    │Catalog │    │ Orders │
    │Service │    │Service │    │Service │
    └────────┘    └────────┘    └────────┘
```

## Component Design

### 1. Gateway API Project Structure

```
R2.ShopNet.Gateway.API/
├── Program.cs                          # Entry point, middleware pipeline
├── appsettings.json                    # Configuration
├── appsettings.Development.json        # Dev overrides
├── Services/
│   ├── ConsulServiceDiscoveryProvider.cs   # IProxyConfigProvider impl
│   ├── ConsulHealthCheckPublisher.cs       # Registers gateway with Consul
│   └── ConfigurationChangeTokenSource.cs   # Triggers YARP reloads
├── Configuration/
│   ├── ConsulOptions.cs                # Consul connection settings
│   ├── GatewayOptions.cs               # Gateway-specific settings
│   └── RouteConfig.cs                  # Route definitions
├── Middleware/
│   ├── CorrelationIdMiddleware.cs      # Adds X-Correlation-ID header
│   ├── RequestLoggingMiddleware.cs     # Logs requests/responses
│   └── ErrorHandlingMiddleware.cs      # Global error handler
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI registration helpers
```

### 2. Consul Service Discovery Provider

#### Purpose
Implements YARP's `IProxyConfigProvider` to dynamically query Consul for healthy service instances and generate YARP routing configuration.

#### Implementation Details

```csharp
namespace R2.ShopNet.Gateway.API.Services;

public sealed class ConsulServiceDiscoveryProvider : IProxyConfigProvider, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;
    private readonly ILogger<ConsulServiceDiscoveryProvider> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConfigurationChangeTokenSource _changeTokenSource;
    
    private volatile IProxyConfig _config;

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
            var clusters = new Dictionary<string, ClusterConfig>();

            // Query Consul for each configured service
            foreach (var serviceMapping in _consulOptions.CurrentValue.ServiceMappings)
            {
                var instances = await GetHealthyServiceInstancesAsync(serviceMapping.ServiceName);
                
                if (instances.Any())
                {
                    // Create YARP cluster with Consul-discovered destinations
                    var destinations = instances.Select((instance, index) => 
                        new DestinationConfig
                        {
                            Address = $"https://{instance.Address}:{instance.Port}"
                        }).ToDictionary(d => Guid.NewGuid().ToString());

                    clusters[serviceMapping.ClusterId] = new ClusterConfig
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

                    // Create route for this service
                    routes.Add(new RouteConfig
                    {
                        RouteId = serviceMapping.RouteId,
                        ClusterId = serviceMapping.ClusterId,
                        Match = new RouteMatch
                        {
                            Path = serviceMapping.PathPattern
                        },
                        Transforms = serviceMapping.Transforms
                    });
                }
                else
                {
                    _logger.LogWarning("No healthy instances found for service {ServiceName}", 
                        serviceMapping.ServiceName);
                }
            }

            _config = new ProxyConfig(routes, clusters);
            _changeTokenSource.SignalChange();
            
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
        var url = $"{consulAddress}/v1/health/service/{serviceName}?passing";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var healthChecks = JsonSerializer.Deserialize<List<ConsulHealthCheck>>(json);

        return healthChecks
            .Select(h => new ServiceInstance
            {
                ServiceId = h.Service.ID,
                ServiceName = h.Service.Service,
                Address = h.Service.Address,
                Port = h.Service.Port,
                Tags = h.Service.Tags
            })
            .ToList();
    }

    private async Task StartRefreshLoopAsync()
    {
        var refreshInterval = TimeSpan.FromSeconds(30);
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(refreshInterval, _cancellationTokenSource.Token);
                await RefreshConfigurationAsync();
            }
            catch (TaskCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in refresh loop");
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
```

#### Configuration Model

```csharp
public sealed class ConsulOptions
{
    public string Address { get; set; } = "http://localhost:8500";
    public List<ServiceMapping> ServiceMappings { get; set; } = new();
}

public sealed class ServiceMapping
{
    public string RouteId { get; set; } = string.Empty;
    public string ClusterId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;  // Consul service name
    public string PathPattern { get; set; } = string.Empty;  // e.g., "/api/identity/{**catch-all}"
    public List<Dictionary<string, string>>? Transforms { get; set; }
}
```

#### appsettings.json Example

```json
{
  "Consul": {
    "Address": "http://localhost:8500",
    "ServiceMappings": [
      {
        "RouteId": "identity-route",
        "ClusterId": "identity-cluster",
        "ServiceName": "identity-service",
        "PathPattern": "/api/identity/{**catch-all}",
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      },
      {
        "RouteId": "catalog-route",
        "ClusterId": "catalog-cluster",
        "ServiceName": "catalog-service",
        "PathPattern": "/api/catalog/{**catch-all}",
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      }
    ]
  }
}
```

### 3. Middleware Pipeline

The gateway uses a carefully ordered middleware pipeline:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add custom Consul provider
builder.Services.AddSingleton<IProxyConfigProvider, ConsulServiceDiscoveryProvider>();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",  // Angular dev server
                "https://admin.shopnet.local"  // Production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<ConsulHealthCheck>("consul");

var app = builder.Build();

// Middleware pipeline (ORDER MATTERS!)
app.UseCorrelationId();           // 1. Add correlation ID first
app.UseRequestLogging();          // 2. Log after correlation ID
app.UseErrorHandling();           // 3. Global error handler
app.UseCors();                    // 4. CORS before auth
app.UseAuthentication();          // 5. Auth before authorization
app.UseAuthorization();           // 6. Authorization
app.UseRateLimiter();             // 7. Rate limiting
app.MapReverseProxy();            // 8. YARP proxy (last)

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

app.Run();
```

### 4. Health Checks

#### Gateway Health Check
```csharp
public sealed class ConsulHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("consul");
            var response = await client.GetAsync(
                $"{_consulOptions.CurrentValue.Address}/v1/status/leader",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Consul is reachable")
                : HealthCheckResult.Unhealthy("Consul returned non-success status");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot reach Consul", ex);
        }
    }
}
```

#### Consul Registration
```csharp
public sealed class ConsulHealthCheckPublisher : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ConsulOptions> _consulOptions;
    private readonly IConfiguration _configuration;
    private string? _serviceId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceId = $"gateway-{Environment.MachineName}-{Guid.NewGuid():N}";
        
        var registration = new
        {
            ID = _serviceId,
            Name = "gateway",
            Address = "localhost",
            Port = 5001,
            Tags = new[] { "api-gateway", "yarp" },
            Check = new
            {
                HTTP = "https://localhost:5001/health",
                Interval = "30s",
                Timeout = "10s"
            }
        };

        using var client = _httpClientFactory.CreateClient("consul");
        var json = JsonSerializer.Serialize(registration);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        await client.PutAsync(
            $"{_consulOptions.CurrentValue.Address}/v1/agent/service/register",
            content,
            cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceId is null) return;

        using var client = _httpClientFactory.CreateClient("consul");
        await client.PutAsync(
            $"{_consulOptions.CurrentValue.Address}/v1/agent/service/deregister/{_serviceId}",
            null,
            cancellationToken);
    }
}
```

## Authentication Flow

### JWT Validation
1. Client sends request with `Authorization: Bearer <token>` header
2. Gateway validates JWT signature and claims
3. If valid, request is forwarded to backend service with same token
4. If invalid, gateway returns 401 Unauthorized

### Token Propagation
```csharp
// Gateway forwards the Authorization header to backend services
// No additional configuration needed - YARP forwards all headers by default
```

## Routing Strategy

### Path-Based Routing
Each microservice is mapped to a specific path prefix:

| Path Pattern | Service | Example |
|--------------|---------|---------|
| `/api/identity/**` | Identity Service | `/api/identity/users` |
| `/api/catalog/**` | Catalog Service | `/api/catalog/products` |
| `/api/orders/**` | Order Service | `/api/orders/123` |

### Path Transformation
The gateway strips the service prefix before forwarding:

- Request: `GET https://api.shopnet.local/api/identity/users`
- Forwarded to Identity Service: `GET https://identity-service:5002/api/users`

## Load Balancing

### Strategy
Uses round-robin load balancing across healthy service instances discovered from Consul.

### Health Checking
- **Passive**: YARP monitors response codes; marks destination unhealthy after consecutive failures
- **Active**: Gateway sends periodic health check requests to `/health` endpoint

## Error Handling

### Scenarios

#### 1. Service Unavailable (503)
- All instances of a service are down
- Response: `503 Service Unavailable` with retry-after header

#### 2. Service Not Found (502)
- Service not registered in Consul
- Response: `502 Bad Gateway` with error details

#### 3. Timeout (504)
- Service didn't respond within timeout
- Response: `504 Gateway Timeout`

#### 4. Authentication Failed (401)
- Invalid or missing JWT token
- Response: `401 Unauthorized`

### Error Response Format
```json
{
  "error": {
    "code": "SERVICE_UNAVAILABLE",
    "message": "The identity-service is currently unavailable",
    "timestamp": "2025-10-19T10:30:00Z",
    "correlationId": "abc123def456"
  }
}
```

## Performance Considerations

### Expected Latency
- **Target**: < 10ms overhead
- **Baseline**: Direct service call ~50ms
- **With Gateway**: ~60ms total

### Throughput
- **Target**: 1,000 requests/second per gateway instance
- **Scaling**: Deploy multiple gateway instances behind load balancer

### Connection Management
- Use HTTP/2 for multiplexing
- Keep-alive connections to backend services
- Connection pooling via HttpClientFactory

## Monitoring & Observability

### Metrics (OpenTelemetry)
- Request count by route
- Request duration (P50, P95, P99)
- Error rate by status code
- Active requests
- Consul discovery refresh duration

### Logs (Serilog)
- Request/response logging with correlation IDs
- Service discovery events
- Health check failures
- Authentication failures

### Tracing (Jaeger)
- Distributed traces across gateway and services
- Span for gateway processing
- Span for each backend service call

## Security Considerations

### TLS/HTTPS
- Gateway terminates TLS
- Backend communication can use HTTP (internal network) or HTTPS

### JWT Validation
- Validate signature using public key from Identity Service
- Check issuer, audience, expiration
- Validate custom claims if needed

### Rate Limiting
- 100 requests per minute per user (authenticated)
- 20 requests per minute per IP (anonymous)

### CORS
- Whitelist specific origins (no wildcards in production)
- Credentials allowed for authenticated requests

## Deployment Strategy

### Development
- Run gateway locally alongside services
- Use Aspire for orchestration
- Consul runs in Docker container

### Production
- Deploy multiple gateway instances (3+ for HA)
- Use external load balancer (Nginx, HAProxy, or cloud LB)
- Consul cluster (3+ nodes)
- Monitor with Prometheus/Grafana

### Rollout Plan
1. Deploy gateway to dev environment
2. Test with Postman/automated tests
3. Point Angular admin portal to gateway
4. Monitor for 1 week
5. Deploy to staging
6. Deploy to production
7. Gradually migrate all clients

## Future Enhancements

### Phase 2 (Post-MVP)
- [ ] GraphQL gateway support
- [ ] WebSocket proxying
- [ ] Response caching (Redis)
- [ ] Request transformation (modify body/headers)
- [ ] Circuit breaker pattern

### Phase 3 (Advanced)
- [ ] API versioning support (path/header-based)
- [ ] Request aggregation (combine multiple service calls)
- [ ] gRPC proxying
- [ ] OAuth2 token exchange
- [ ] Advanced rate limiting (by API key, custom rules)

## References

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [YARP Configuration](https://microsoft.github.io/reverse-proxy/articles/config-files.html)
- [Consul Health Checks](https://developer.hashicorp.com/consul/docs/services/usage/checks)
- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
