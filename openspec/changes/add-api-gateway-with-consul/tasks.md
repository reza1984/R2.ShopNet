# Implementation Tasks: API Gateway with Consul Integration

**Change ID:** `add-api-gateway-with-consul`  
**Status:** Not Started  

## Task Checklist

### Phase 1: Gateway Foundation ⏳ Not Started
- [ ] Create R2.ShopNet.Gateway.API project
  - [ ] Create project directory structure
  - [ ] Add to R2.ShopNet.sln
  - [ ] Add necessary NuGet packages (YARP, Serilog, etc.)
  - [ ] Create Program.cs with minimal YARP configuration
  - [ ] Create appsettings.json and appsettings.Development.json

- [ ] Configure basic YARP reverse proxy
  - [ ] Add YARP services in Program.cs
  - [ ] Define initial route for Identity Service (hardcoded)
  - [ ] Configure request forwarding
  - [ ] Test basic proxy functionality

- [ ] Add health checks
  - [ ] Create /health endpoint
  - [ ] Create /ready endpoint
  - [ ] Add dependency health checks
  - [ ] Configure health check UI (optional)

- [ ] Test with Identity Service
  - [ ] Run Identity Service locally
  - [ ] Start Gateway
  - [ ] Test GET /api/identity/users through gateway
  - [ ] Verify request forwarding works
  - [ ] Check response headers

### Phase 2: Consul Integration ⏳ Not Started
- [ ] Create Consul service discovery provider
  - [ ] Create Services/ConsulServiceDiscoveryProvider.cs
  - [ ] Implement IProxyConfigProvider interface
  - [ ] Add Consul HTTP client configuration
  - [ ] Create ConsulServiceDiscoveryOptions class

- [ ] Implement service discovery logic
  - [ ] Query Consul /v1/health/service/{name}?passing
  - [ ] Parse Consul response (service instances)
  - [ ] Map Consul services to YARP cluster destinations
  - [ ] Handle service not found scenarios
  - [ ] Add error handling and logging

- [ ] Implement configuration refresh
  - [ ] Create background service for periodic refresh
  - [ ] Set refresh interval (30 seconds)
  - [ ] Implement IChangeToken for YARP notifications
  - [ ] Add graceful degradation (cache last known config)
  - [ ] Log configuration changes

- [ ] Register Gateway with Consul
  - [ ] Create Consul registration service
  - [ ] Register gateway on startup
  - [ ] Configure health check endpoint
  - [ ] Implement graceful deregistration on shutdown
  - [ ] Test service appears in Consul UI

- [ ] Update route configuration
  - [ ] Replace hardcoded destinations with Consul lookups
  - [ ] Add Consul section to appsettings.json
  - [ ] Configure service name mappings
  - [ ] Add fallback configuration
  - [ ] Test dynamic service discovery

### Phase 3: Cross-Cutting Concerns ⏳ Not Started
- [ ] Add JWT authentication
  - [ ] Add Microsoft.AspNetCore.Authentication.JwtBearer package
  - [ ] Configure JWT validation parameters
  - [ ] Add authentication middleware
  - [ ] Test protected endpoints
  - [ ] Configure auth bypass for health checks

- [ ] Configure CORS policy
  - [ ] Add CORS services
  - [ ] Define allowed origins (Angular dev server, production URLs)
  - [ ] Configure allowed headers and methods
  - [ ] Add CORS middleware
  - [ ] Test CORS from Angular admin portal

- [ ] Add rate limiting
  - [ ] Add AspNetCoreRateLimit package
  - [ ] Configure rate limit policies
  - [ ] Add rate limiting middleware
  - [ ] Test rate limit responses (429)
  - [ ] Add rate limit headers

- [ ] Implement request/response logging
  - [ ] Configure Serilog with enrichers
  - [ ] Add request logging middleware
  - [ ] Log request/response metadata (method, path, status, duration)
  - [ ] Add correlation ID to requests
  - [ ] Configure structured logging output

- [ ] Add OpenTelemetry tracing
  - [ ] Add OpenTelemetry packages
  - [ ] Configure tracing provider
  - [ ] Add YARP instrumentation
  - [ ] Configure trace export (Jaeger)
  - [ ] Test distributed tracing

### Phase 4: Aspire Integration ⏳ Not Started
- [ ] Add Gateway to AppHost
  - [ ] Add gateway project reference to AppHost.csproj
  - [ ] Add gateway to AppHost.cs builder
  - [ ] Configure gateway to reference Consul resource
  - [ ] Configure gateway to reference Identity Service
  - [ ] Set gateway ports (5000 HTTP, 5001 HTTPS)

- [ ] Configure service references
  - [ ] Add .WithReference(consul) to gateway
  - [ ] Configure Consul address environment variable
  - [ ] Add .WithReference(identityService) for direct testing
  - [ ] Test gateway starts with Aspire

- [ ] Update Angular configuration
  - [ ] Update environment.development.ts apiUrl to gateway (http://localhost:5001)
  - [ ] Update environment.production.ts apiUrl to gateway
  - [ ] Test Angular admin portal connects to gateway
  - [ ] Verify authentication flow works
  - [ ] Test user management features end-to-end

- [ ] Test Aspire orchestration
  - [ ] Run entire stack with Aspire
  - [ ] Verify all services register with Consul
  - [ ] Check gateway discovers services
  - [ ] Test requests route correctly
  - [ ] Check Aspire dashboard shows all services healthy

### Phase 5: Documentation & Testing ⏳ Not Started
- [ ] Write API Gateway documentation
  - [ ] Create docs/API-Gateway-Architecture.md
  - [ ] Document routing patterns
  - [ ] Document Consul integration
  - [ ] Document authentication flow
  - [ ] Add troubleshooting guide

- [ ] Create integration tests
  - [ ] Create R2.ShopNet.Gateway.API.Tests project
  - [ ] Add tests for routing logic
  - [ ] Add tests for Consul service discovery
  - [ ] Add tests for authentication
  - [ ] Add tests for CORS
  - [ ] Add tests for rate limiting

- [ ] Performance testing
  - [ ] Create load test scenarios (k6 or JMeter)
  - [ ] Measure baseline latency (direct to service)
  - [ ] Measure gateway latency
  - [ ] Verify < 10ms overhead
  - [ ] Test 1000 req/sec throughput

- [ ] Update project documentation
  - [ ] Update README.md with gateway information
  - [ ] Update docs/Local-Infrastructure-Setup.md
  - [ ] Update QUICKSTART-ADMIN.md with gateway URLs
  - [ ] Add gateway troubleshooting to docs
  - [ ] Document emergency fallback procedures

- [ ] Security review
  - [ ] Review JWT validation logic
  - [ ] Review CORS configuration
  - [ ] Check for sensitive data in logs
  - [ ] Verify rate limiting prevents abuse
  - [ ] Review error messages (no info disclosure)

## Definition of Done

- [ ] All tasks above marked complete
- [ ] Gateway routes requests to Identity Service successfully
- [ ] Consul integration discovers services dynamically
- [ ] Angular admin portal works through gateway
- [ ] All tests pass (unit, integration, performance)
- [ ] Documentation complete and reviewed
- [ ] Code reviewed and approved
- [ ] Deployed to development environment
- [ ] Monitoring and alerts configured

## Estimated Effort

- **Phase 1**: 8 hours
- **Phase 2**: 12 hours
- **Phase 3**: 8 hours
- **Phase 4**: 4 hours
- **Phase 5**: 8 hours
- **Total**: ~40 hours (1 week)

## Dependencies

- Consul must be running and accessible
- Identity Service must register with Consul
- Angular admin portal must be functional

## Blockers

None currently identified

## Notes

- Start with Phase 1 to get quick feedback
- Phase 2 is the most complex (Consul integration)
- Phase 3 can be done incrementally (add features one at a time)
- Phase 4 requires all previous phases complete
- Phase 5 should run in parallel with development
