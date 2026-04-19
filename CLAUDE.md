<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

R2.ShopNet is a comprehensive e-commerce microservices platform built with .NET 9, Angular 20, and designed for on-premises deployment. The platform consists of 13 microservices and 4 web applications serving an e-commerce ecosystem.

**Key Architecture:**
- Clean Architecture with CQRS pattern
- Microservices communicating via RabbitMQ
- Consul for service discovery and configuration management
- PostgreSQL, Redis, Elasticsearch, MinIO (all self-hosted)
- YARP API Gateway with Consul integration
- Angular 20 with Signals and Zoneless change detection

## Essential Commands

### .NET Backend

**Build the solution:**
```bash
dotnet build R2.ShopNet.sln
```

**Build specific service:**
```bash
dotnet build src/Services/Catalog/R2.ShopNet.Catalog.API/R2.ShopNet.Catalog.API.csproj
```

**Run tests:**
```bash
dotnet test                                    # All tests
dotnet test --filter "FullyQualifiedName~Catalog"  # Specific service
dotnet test /p:CollectCoverage=true           # With coverage
```

**Database migrations (from service API directory):**
```bash
# Create migration
dotnet ef migrations add MigrationName --project ../R2.ShopNet.Catalog.Infrastructure

# Apply migration
dotnet ef database update --project ../R2.ShopNet.Catalog.Infrastructure

# Generate SQL script
dotnet ef migrations script --project ../R2.ShopNet.Catalog.Infrastructure --output migration.sql
```

**Run a service:**
```bash
cd src/Services/Catalog/R2.ShopNet.Catalog.API
dotnet run
```

### Angular Frontend

**Install dependencies:**
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm install
```

**Run development server:**
```bash
npm start                    # Admin portal (uses SSL)
ng serve                     # Without SSL
ng serve --port 4201         # Custom port
```

**Build for production:**
```bash
npm run build
ng build --configuration production
```

**Run tests:**
```bash
npm test                     # Run Jasmine tests
ng test                      # Run with watch mode
```

**TypeScript type checking:**
```bash
npx tsc --noEmit
```

### Infrastructure

**Start all infrastructure services:**
```bash
docker-compose up -d
```

**Check service health:**
```bash
docker-compose ps
```

**Stop services:**
```bash
docker-compose down
```

**View logs:**
```bash
docker-compose logs -f postgres    # PostgreSQL
docker-compose logs -f consul      # Consul
docker-compose logs -f minio       # MinIO
```

**Consul UI:**
```
http://localhost:8500
```

**MinIO Console:**
```
http://localhost:9001
User: minioadmin / Pass: minioadmin
```

## Project Architecture

### Core Patterns

**GUID v7 for Entity IDs:**
All entities use GUID Version 7 (RFC 9562) for time-ordered, database-friendly identifiers via `GuidGenerator.NewGuidV7()`. This is automatically applied in `BaseEntity`.

**CQRS with Custom Implementation:**
- Custom in-house CQRS using `ICommand<TResponse>` and `ICommandHandler<TCommand, TResponse>`
- No external dependencies (MediatR, etc.)
- Commands modify state, Queries read data
- Separate read/write data models
- Commands in `Application/Commands/`, Queries in `Application/Queries/`

**Result Pattern for Error Handling:**
```csharp
// Success
return Result<ProductDto>.Success(dto);

// Failure
return Result<ProductDto>.Failure(
    Error.NotFound("Product.NotFound", "Product not found")
);
```

Error types: `NotFound`, `Validation`, `Conflict`, `Unauthorized`, `Forbidden`, `Failure`

**Manual DTO Mapping (No AutoMapper):**
- Use EF Core projection with `.Select()` in queries for optimal performance
- Extension methods (`.ToDto()`) for reusable mappings
- Direct property mapping in command handlers
- This approach provides better performance and easier debugging

**Entity Framework Core Usage:**
- Commands: Use tracked entities with `_context.SaveChangesAsync()`
- Queries: Use `.AsNoTracking()` for read operations
- Complex queries: Use `.AsSplitQuery()` to avoid cartesian explosion
- High-frequency reads: Use `EF.CompileAsyncQuery()` for compiled queries
- All data access through EF Core (no Dapper or raw SQL unless necessary)

### Microservices Structure

Each service follows this structure:
```
src/Services/{ServiceName}/
├── R2.ShopNet.{ServiceName}.API/           # Web API, controllers, middleware
├── R2.ShopNet.{ServiceName}.Application/   # CQRS handlers, DTOs, business logic
├── R2.ShopNet.{ServiceName}.Domain/        # Domain entities, value objects, interfaces
└── R2.ShopNet.{ServiceName}.Infrastructure/ # EF Core, repositories, external services
```

**Framework Libraries:**
```
src/Framework/
├── R2.ShopNet.Framework.Common/            # BaseEntity, Result pattern, GUID v7
├── R2.ShopNet.Framework.CQRS/              # Custom CQRS implementation
├── R2.ShopNet.Framework.Events/            # Domain events
├── R2.ShopNet.Framework.Persistence/       # EF Core base repository
├── R2.ShopNet.Framework.ServiceDiscovery/  # Consul integration
├── R2.ShopNet.Framework.Configuration/     # Consul KV configuration
└── R2.ShopNet.Framework.Validation/        # Custom validation framework
```

### Service Discovery with Consul

**Key Concepts:**
- All services register with Consul on startup via `ConsulServiceRegistration` hosted service
- Health checks at `/health` endpoint (HTTP checks every 10 seconds)
- Services discover each other dynamically using `IServiceDiscovery`
- YARP API Gateway uses Consul for load balancing and routing
- Configuration stored in Consul KV store with prefix `config/shopnet/{service-name}/`

**Service Registration Pattern:**
```csharp
// In Program.cs
builder.Services.AddSingleton<IConsulClient>(p =>
    new ConsulClient(config =>
        config.Address = new Uri("http://localhost:8500")
    )
);
builder.Services.AddHostedService<ConsulServiceRegistration>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());
```

**Service Discovery Usage:**
```csharp
// Resolve service URL dynamically
var serviceUri = await _serviceDiscovery.GetServiceUriAsync("catalog-service");
var requestUri = new Uri(serviceUri, "/api/products");
```

### Angular 20 Architecture

**Standalone Components (No NgModules):**
All components are standalone with explicit imports. No module files.

**Signals for State Management:**
```typescript
// Service with signals
readonly products = signal<Product[]>([]);
readonly loading = signal(false);
readonly totalProducts = computed(() => this.products().length);

// Update state
this.products.set(newProducts);
this.products.update(current => [...current, newProduct]);
```

**Zoneless Change Detection:**
Enabled via `provideZonelessChangeDetection()` in `main.ts` for better performance.

**Modern Template Syntax:**
```html
@if (loading()) {
  <div>Loading...</div>
}

@for (product of products(); track product.id) {
  <div>{{ product.name }}</div>
}
```

**Project Structure:**
```
src/Web/R2.ShopNet.Web.Admin/
├── src/app/
│   ├── core/          # Singleton services, guards, interceptors
│   ├── features/      # Feature components (products, orders, auth)
│   ├── shared/        # Shared components, directives, pipes
│   ├── app.component.ts
│   └── app.routes.ts
```

## Important Conventions

### Naming Rules

**ALL projects must start with `R2.ShopNet` prefix:**
- ✅ `R2.ShopNet.Catalog.API`
- ❌ `Catalog.API`

**Namespaces follow structure:**
```
R2.ShopNet.{ServiceName}.{Layer}.{Feature}
Example: R2.ShopNet.Catalog.Application.Commands.CreateProduct
```

**Methods:**
- Async methods end with `Async`: `GetByIdAsync`, `SaveAsync`
- Use PascalCase for all methods

**Variables:**
- Local: camelCase (`userId`, `productName`)
- Private fields: `_repository`, `_logger`

### OpenSpec Workflow

This project uses OpenSpec for spec-driven development:

**Before implementing features:**
1. Check `openspec/specs/` for existing capabilities
2. Run `openspec list` to see active changes
3. Create proposal in `openspec/changes/{change-id}/`
4. Validate with `openspec validate {change-id} --strict`
5. Wait for approval before implementing

**Key files in proposals:**
- `proposal.md` - Why and what changes
- `tasks.md` - Implementation checklist
- `design.md` - Technical decisions (when needed)
- `specs/{capability}/spec.md` - Requirement deltas

**Don't create proposals for:**
- Bug fixes (restoring intended behavior)
- Typos, formatting, comments
- Non-breaking dependency updates
- Configuration changes

### Testing Patterns

**Test naming:**
```csharp
[Fact]
public async Task HandleAsync_WhenValidCommand_ShouldCreateProduct()
{
    // Arrange
    var command = new CreateProductCommand { Name = "Test" };

    // Act
    var result = await _handler.HandleAsync(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
}
```

**Integration tests with Testcontainers:**
- Use Testcontainers for PostgreSQL, Redis, RabbitMQ
- Clean state between tests
- Use `WebApplicationFactory` for API testing

### Configuration Management

**appsettings.json structure:**
```json
{
  "Consul": {
    "Address": "http://localhost:8500",
    "ServiceName": "catalog-service",
    "ServicePort": 5004
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=shopnet_catalog;..."
  },
  "Redis": {
    "KeyValue": {
      "ConnectionString": "localhost:6379"
    }
  }
}
```

**Environment-specific overrides:**
Use `appsettings.Development.json`, `appsettings.Production.json`

### Service Ports

| Service | Port |
|---------|------|
| API Gateway | 5000 |
| Identity | 5001 |
| Catalog | 5002 |
| Inventory | 5003 |
| Shopping App | 4200 |
| Admin Portal | 4201 |
| Warehouse App | 4202 |
| Delivery App | 4203 |
| Consul | 8500 |
| PostgreSQL | 5432 |
| Redis | 6379 |
| RabbitMQ | 5672, 15672 (UI) |
| MinIO | 9000, 9001 (UI) |
| Elasticsearch | 9200 |

## Key Design Decisions

### Why GUID v7?
- 30-50% faster INSERTs vs random GUIDv4
- Sequential insertion reduces B-tree fragmentation
- Better cache locality and index performance
- Time-ordered, naturally sortable by creation time

### Why Custom CQRS?
- No external dependencies, full control
- Simpler than MediatR for this use case
- Easier to understand and debug
- Tailored to project needs

### Why Manual Mapping?
- Better performance (no reflection)
- Type-safe at compile time
- Easier debugging
- EF Core projection optimizes database queries

### Why Consul?
- Dynamic service discovery (no hardcoded URLs)
- Health checking with automatic deregistration
- Centralized configuration management
- Integrates with YARP for API Gateway routing

### Why Angular Signals over NgRx?
- Built into Angular 20 (no external dependency)
- Simpler mental model
- Better performance with zoneless change detection
- Less boilerplate code

## Common Tasks

### Adding a new microservice

1. Create project structure:
```bash
mkdir -p src/Services/NewService
cd src/Services/NewService
dotnet new webapi -n R2.ShopNet.NewService.API
dotnet new classlib -n R2.ShopNet.NewService.Application
dotnet new classlib -n R2.ShopNet.NewService.Domain
dotnet new classlib -n R2.ShopNet.NewService.Infrastructure
```

2. Add references:
```bash
cd R2.ShopNet.NewService.API
dotnet add reference ../R2.ShopNet.NewService.Application
dotnet add reference ../R2.ShopNet.NewService.Infrastructure

cd ../R2.ShopNet.NewService.Application
dotnet add reference ../R2.ShopNet.NewService.Domain

cd ../R2.ShopNet.NewService.Infrastructure
dotnet add reference ../R2.ShopNet.NewService.Domain
```

3. Add to solution:
```bash
cd ../../..
dotnet sln add src/Services/NewService/R2.ShopNet.NewService.API/R2.ShopNet.NewService.API.csproj
dotnet sln add src/Services/NewService/R2.ShopNet.NewService.Application/R2.ShopNet.NewService.Application.csproj
dotnet sln add src/Services/NewService/R2.ShopNet.NewService.Domain/R2.ShopNet.NewService.Domain.csproj
dotnet sln add src/Services/NewService/R2.ShopNet.NewService.Infrastructure/R2.ShopNet.NewService.Infrastructure.csproj
```

4. Reference framework libraries and configure Consul registration

### Adding a new command

1. Create folder: `Application/Commands/{CommandName}/`
2. Create command class implementing `ICommand<TResponse>`
3. Create handler class implementing `ICommandHandler<TCommand, TResponse>`
4. Create validator class if needed
5. Register handler in DI container

### Adding a new query

1. Create folder: `Application/Queries/{QueryName}/`
2. Create query class implementing `IQuery<TResponse>`
3. Create handler using `.AsNoTracking()` and projection to DTO
4. Use `EF.CompileAsyncQuery()` for frequently used queries

### Working with MinIO

MinIO is configured with service-specific buckets and IAM policies:
- `product-images` - Catalog service (user: `catalog-service`)
- `user-avatars` - Identity service (user: `identity-service`)
- `media-files` - Media service (user: `media-service`)

All buckets are private; use presigned URLs for access.

## Reference Documentation

- [openspec/AGENTS.md](openspec/AGENTS.md) - Spec-driven development workflow
- [openspec/project.md](openspec/project.md) - Comprehensive project conventions and patterns
- [src/Framework/R2.ShopNet.Framework.Common/README.md](src/Framework/R2.ShopNet.Framework.Common/README.md) - GUID v7 and Result pattern usage
- [CONTRIBUTING.md](CONTRIBUTING.md) - Code style and commit conventions
- [README.md](README.md) - Project overview and getting started

## Notes

- Always use EF Core `.AsNoTracking()` for queries
- Always use `.Select()` projection to DTOs in queries when possible
- Use Polly for retry and circuit breaker policies on external calls
- Redis password is `redis123` in docker-compose
- PostgreSQL default password is `postgres` in docker-compose
- MinIO credentials in docker-compose can be overridden with environment variables