# Change Proposal: Add API Gateway with Consul Integration

**Change ID:** `add-api-gateway-with-consul`  
**Created:** 2025-10-19  
**Status:** Draft  
**Type:** New Capability  

## Summary

Implement a centralized API Gateway using YARP (Yet Another Reverse Proxy) that integrates with the existing Consul service discovery infrastructure. This gateway will serve as a single entry point for all client applications (Angular Admin Portal, Shopping Site, Mobile Apps) and route requests to backend microservices dynamically based on Consul service registry.

## Motivation

### Current State
- Angular Admin Portal has hardcoded API URLs in environment files
- Each microservice exposes its own HTTP endpoint
- No centralized point for cross-cutting concerns (authentication, rate limiting, logging, CORS)
- Service URLs must be manually updated when services move or scale
- Frontend applications need to know about every backend service

### Problems
1. **Tight Coupling**: Frontend apps are tightly coupled to backend service locations
2. **Configuration Management**: Each client needs separate configuration for each service
3. **No Unified Security**: Auth, CORS, and security policies are duplicated across services
4. **Service Discovery Gap**: Consul is set up but not leveraged by clients
5. **Scalability Issues**: Adding new services requires frontend changes

### Proposed Solution
Create a dedicated API Gateway service that:
- Acts as a reverse proxy using YARP
- Queries Consul for service locations dynamically
- Provides a single, well-known URL for all client applications
- Handles cross-cutting concerns (auth, CORS, rate limiting, logging)
- Enables zero-downtime service updates and scaling

## Goals

### Primary Goals
1. **Single Entry Point**: All client applications connect to one gateway URL
2. **Dynamic Service Discovery**: Gateway discovers services via Consul automatically
3. **Transparent Routing**: Route requests to appropriate services based on path patterns
4. **Zero Frontend Changes**: Services can move/scale without client reconfiguration

### Secondary Goals
1. **Centralized Auth**: JWT validation at gateway level
2. **Unified CORS**: Single CORS policy for all services
3. **Request/Response Logging**: Centralized observability
4. **Rate Limiting**: Protect backend services from abuse
5. **Health Checking**: Gateway health checks via Consul

## Design Overview

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Client Applications                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Admin Portal │  │ Shopping Web │  │  Mobile App  │     │
│  │  (Angular)   │  │   (Angular)  │  │   (Flutter)  │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                  │                  │              │
│         └──────────────────┼──────────────────┘              │
│                            │                                 │
└────────────────────────────┼─────────────────────────────────┘
                             │
                    https://api.shopnet.local
                             │
┌────────────────────────────▼─────────────────────────────────┐
│                       API Gateway (YARP)                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Route Configuration (appsettings.json)                │ │
│  │  - /api/identity/** → identity-service                 │ │
│  │  - /api/catalog/**  → catalog-service                  │ │
│  │  - /api/orders/**   → order-service                    │ │
│  └────────────────────────────────────────────────────────┘ │
│                            │                                  │
│  ┌────────────────────────▼────────────────────────────────┐ │
│  │     Consul Service Discovery Provider                   │ │
│  │     - Queries Consul for service health & locations    │ │
│  │     - Updates routes dynamically                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                            │                                  │
│  ┌────────────────────────▼────────────────────────────────┐ │
│  │  Middleware Pipeline                                    │ │
│  │  1. CORS                                                │ │
│  │  2. Authentication (JWT)                                │ │
│  │  3. Rate Limiting                                       │ │
│  │  4. Request Logging                                     │ │
│  │  5. Health Checks                                       │ │
│  └────────────────────────────────────────────────────────┘ │
└────────────────────────────┬─────────────────────────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
      ┌───────────┐  ┌───────────┐  ┌───────────┐
      │ Identity  │  │  Catalog  │  │  Orders   │
      │  Service  │  │  Service  │  │  Service  │
      │           │  │           │  │           │
      │ (Consul)  │  │ (Consul)  │  │ (Consul)  │
      └───────────┘  └───────────┘  └───────────┘
```

### Component Details

#### 1. API Gateway Service (`R2.ShopNet.Gateway.API`)
- **Framework**: ASP.NET Core 9.0 with YARP
- **Port**: 5000 (HTTP), 5001 (HTTPS)
- **Responsibilities**:
  - Reverse proxy to backend services
  - Service discovery via Consul
  - JWT authentication
  - CORS policy enforcement
  - Request/response logging
  - Health checks

#### 2. Consul Integration
- **Service Provider**: Custom `ConsulServiceDiscoveryProvider` implementing YARP's `IProxyConfigProvider`
- **Discovery Pattern**:
  1. Gateway queries Consul HTTP API (`/v1/health/service/{name}?passing`)
  2. Retrieves healthy service instances with addresses
  3. Updates YARP route configuration dynamically
  4. Refreshes every 30 seconds or on Consul watch trigger

#### 3. Route Configuration
Routes defined in `appsettings.json` with Consul service names:

```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": {
          "Path": "/api/identity/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      },
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/api/catalog/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Consul": {
          "ServiceName": "identity-service",
          "DataCenter": "dc1"
        },
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

### Technology Choices

#### Why YARP?
- **Microsoft Official**: Built and maintained by Microsoft
- **High Performance**: Written in C# with async/await, minimal overhead
- **Flexible**: Programmatic configuration, custom middleware
- **Well-Documented**: Extensive documentation and samples
- **Active Development**: Regular updates, .NET 9 support
- **Free**: MIT licensed

#### Alternatives Considered
- **Ocelot**: Mature but less performant, fewer updates
- **Envoy**: Powerful but complex, requires additional infrastructure
- **Kong/Nginx**: External tools, harder to integrate with .NET ecosystem

## Implementation Plan

### Phase 1: Gateway Foundation (Day 1-2)
1. Create `R2.ShopNet.Gateway.API` project
2. Add YARP NuGet packages
3. Configure basic reverse proxy
4. Add health checks endpoint
5. Test with hardcoded Identity Service route

### Phase 2: Consul Integration (Day 3-4)
1. Create `ConsulServiceDiscoveryProvider` implementing `IProxyConfigProvider`
2. Query Consul HTTP API for service health
3. Map Consul services to YARP clusters
4. Implement configuration refresh (30s interval)
5. Add Consul health check registration for gateway itself

### Phase 3: Middleware & Cross-Cutting Concerns (Day 5)
1. Add JWT authentication middleware
2. Configure CORS policy
3. Add rate limiting (AspNetCoreRateLimit)
4. Implement request/response logging with Serilog
5. Add OpenTelemetry tracing

### Phase 4: Aspire Integration (Day 6)
1. Add gateway to `AppHost.cs`
2. Configure gateway to reference Consul resource
3. Update Angular environment files to use gateway URL
4. Test end-to-end with Aspire dashboard

### Phase 5: Documentation & Testing (Day 7)
1. Write API Gateway usage guide
2. Create integration tests
3. Document routing patterns
4. Update QUICKSTART guides

## Impact Analysis

### Services Affected
- ✅ **R2.ShopNet.AppHost**: Add gateway project reference
- ✅ **R2.ShopNet.Web.Admin**: Update `environment.ts` to use gateway URL
- ✅ **R2.ShopNet.Identity.API**: No changes (already registers with Consul)
- 🆕 **R2.ShopNet.Gateway.API**: New project

### Breaking Changes
- **None**: This is an additive change
- Existing direct service access will continue to work
- Clients can migrate to gateway gradually

### Migration Path
1. Deploy gateway alongside existing services
2. Test gateway routing with postman/curl
3. Update Angular admin portal to use gateway (one service at a time)
4. Monitor for issues
5. Deprecate direct service access after validation period

## Success Criteria

### Functional Requirements
- ✅ Gateway routes requests to Identity Service correctly
- ✅ Consul integration discovers services dynamically
- ✅ JWT authentication works at gateway level
- ✅ CORS allows Angular admin portal requests
- ✅ Health checks report gateway and service health
- ✅ Angular admin portal works with gateway URL

### Non-Functional Requirements
- ✅ Gateway adds < 10ms latency overhead
- ✅ Gateway handles 1000 req/sec per instance
- ✅ Configuration refresh doesn't cause downtime
- ✅ Failed services are removed from routing within 30s
- ✅ Gateway restarts without losing state

### Testing Requirements
- ✅ Integration tests for each route
- ✅ Load tests showing acceptable latency
- ✅ Failover tests (service goes down)
- ✅ Consul unavailability tests (graceful degradation)

## Risks & Mitigations

### Risk 1: Single Point of Failure
**Impact**: If gateway goes down, all clients lose access  
**Mitigation**: 
- Run multiple gateway instances behind load balancer
- Implement health checks for automatic removal
- Keep direct service access as fallback (document emergency procedures)

### Risk 2: Latency Overhead
**Impact**: Gateway adds network hop, increasing latency  
**Mitigation**:
- YARP is highly optimized (< 10ms overhead)
- Use keep-alive connections
- Monitor with OpenTelemetry
- Benchmark before/after

### Risk 3: Consul Dependency
**Impact**: If Consul is down, gateway can't discover services  
**Mitigation**:
- Cache last known good configuration
- Implement graceful degradation (use cached routes)
- Add fallback to static configuration

### Risk 4: Complex Debugging
**Impact**: Issues harder to diagnose with extra hop  
**Mitigation**:
- Comprehensive request/response logging
- Correlation IDs in all requests
- OpenTelemetry distributed tracing
- Detailed error messages in responses

## Open Questions

1. **Load Balancer**: Do we need external LB (Nginx, HAProxy) or rely on Consul DNS?
2. **SSL Termination**: Should gateway handle SSL or pass through to services?
3. **API Versioning**: How to handle different API versions (/v1, /v2)?
4. **Request Size Limits**: What are appropriate limits for file uploads?
5. **Caching**: Should gateway cache responses (GET requests)?

## References

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [Consul Service Discovery](https://developer.hashicorp.com/consul/docs/discovery)
- [API Gateway Pattern](https://microservices.io/patterns/apigateway.html)
- [R2.ShopNet Architecture Documentation](../../docs/PRD.md)

## Approval

- [ ] Technical Lead Review
- [ ] Security Review
- [ ] Performance Review
- [ ] Documentation Review

---

**Next Steps**: 
1. Review and approve this proposal
2. Create detailed `tasks.md` checklist
3. Begin Phase 1 implementation
