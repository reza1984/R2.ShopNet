# Project Context

## Purpose
Build R2.ShopNet, a modern, self-hosted e-commerce and logistics platform that delivers scalability, high performance, and maintainability for comprehensive online shopping operations. The platform leverages microservices architecture and CQRS pattern to provide independent scaling, real-time inventory management, delivery tracking, and multi-application ecosystem.

**Platform Components:**
1. **Shopping Site** - Customer-facing e-commerce website
2. **Warehouse Management App** - Inventory and warehouse operations
3. **Delivery App** - Driver/courier delivery management
4. **Admin Portal** - User management, roles, and permissions

### Goals
- Achieve 99.9% uptime SLA with zero-downtime deployments
- Support 50,000+ concurrent shoppers with sub-second product search
- Process 10,000+ orders per day
- Enable real-time inventory tracking across multiple warehouses
- Provide mobile-first delivery app for drivers
- Support horizontal scaling for independent read/write operations

## Tech Stack

### Backend (.NET)
- **.NET 9.0** (with .NET 10 forward compatibility)
- **.NET Aspire 9.5.1** for orchestration and observability
- **C# 13** for all services
- **ASP.NET Core** for Web APIs
- **Consul** for service discovery, health checking, and configuration management
- **PostgreSQL** for command store (write operations) - self-hosted
- **Redis** for distributed caching - self-hosted
- **Elasticsearch** for full-text search - self-hosted
- **RabbitMQ** for message queue - self-hosted
- **Local File System / MinIO** for media storage (S3-compatible)
- **Docker / Docker Compose** for containerization
- **YARP** (Yet Another Reverse Proxy) for API Gateway with Consul integration
- **OpenIddict** for authentication (free, Apache 2.0 licensed)
- **OpenTelemetry** for distributed tracing
- **Kubernetes (k3s/MicroK8s)** for orchestration (optional, on-premises)

### Frontend (Angular)
- **Angular 20** (latest stable version, released May 2025)
- **TypeScript 5.7+** for type-safe development
- **Angular Standalone Components** (no NgModules)
- **Angular Signals** for reactive state management (stable in v20)
- **Zoneless Change Detection** (stable in v20.2)
- **Server-Side Rendering (SSR)** with route-level render modes (stable in v20)
- **Angular Material 20** for UI components (or custom component library)
- **RxJS 7+** for reactive programming
- **NgRx Signals** or **Angular Signals** for state management (no external state library)
- **Tailwind CSS** or **Angular Material** for styling

### Supporting Libraries
- **Custom CQRS Implementation** - Built in-house using IRequest/IRequestHandler pattern
- **DataAnnotations + Custom Validators** - Built-in .NET validation with custom validation framework
- **Manual DTO Mapping** - Programmatic mapping from entities to DTOs (no external mapper libraries)
- **Serilog** - Structured logging (MIT license)
- **Polly** - Resilience and transient fault handling (BSD license)
- **Entity Framework Core 9** - Complete ORM for all data access (MIT license)
  - EF Core for Commands (write operations)
  - EF Core for Queries (read operations with AsNoTracking)
  - EF Core with Compiled Queries for high-performance reads
  - EF Core with Split Queries for complex queries
- **SkiaSharp** - Image processing (MIT license, free alternative to ImageSharp)
- **xUnit** - Unit testing framework (Apache 2.0 license)
- **Testcontainers** - Integration testing (MIT license)

## Project Conventions

### Code Style

#### Naming Conventions
- **Project Prefix**: All projects MUST start with `R2.ShopNet`
  - ✅ Correct: `R2.ShopNet.Content.API`
  - ❌ Incorrect: `Content.API` or `ShopNet.Content.API`
- **Namespaces**: Follow project structure
  - Format: `R2.ShopNet.{ServiceName}.{Layer}.{Feature}`
  - Example: `R2.ShopNet.Content.Application.Commands.CreateContent`
- **Classes**: PascalCase
  - Commands: `CreateContentCommand`
  - Handlers: `CreateContentCommandHandler`
  - Validators: `CreateContentCommandValidator`
- **Interfaces**: PascalCase with `I` prefix
  - `IContentRepository`, `ICommand<T>`, `ICommandHandler<TCommand, TResponse>`
- **Methods**: PascalCase
  - Async methods must end with `Async`: `GetByIdAsync`, `SaveAsync`
- **Variables**: camelCase
  - Local variables: `contentItem`, `userId`
  - Private fields: `_repository`, `_logger`

#### Code Formatting
- Follow C# coding conventions and .NET naming guidelines
- Use nullable reference types throughout
- Prefer `var` for local variables when type is obvious
- Use file-scoped namespaces (C# 10+)
- Maximum line length: 120 characters
- Use expression-bodied members for simple methods
- Apply EditorConfig rules consistently

**Example:**
```csharp
namespace R2.ShopNet.Catalog.Application.Commands.CreateProduct;

// Custom CQRS interfaces (no external dependencies)
public interface ICommand<TResponse> { }
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public sealed class CreateProductCommand : ICommand<Result<ProductDto>>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public async Task<Result<ProductDto>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // Validation and business logic
        var product = new Product
        {
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            CategoryId = command.CategoryId
        };

        await _productRepository.AddAsync(product, cancellationToken);

        // Manual mapping to DTO
        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId
        };

        return Result<ProductDto>.Success(dto);
    }
}
```

### Architecture Patterns

#### Microservices Architecture
- Each service is independently deployable
- Services communicate via async messaging (RabbitMQ)
- **Service discovery managed by Consul** with health checking
- API Gateway (YARP) handles routing with Consul service discovery integration
- Configuration management via Consul KV store
- Dynamic service registration and deregistration

#### CQRS (Command Query Responsibility Segregation)
- **Commands**: Modify state, processed by command handlers
- **Queries**: Read data, processed by query handlers
- Separate read/write data models
- Event sourcing for critical business events
- Custom in-process command/query dispatcher with pipeline behaviors
- Dependency injection for handler resolution

#### Domain-Driven Design (DDD)
- Rich domain models with business logic
- Aggregate roots enforce consistency boundaries
- Value objects for immutable concepts
- Domain events for cross-aggregate communication
- Repository pattern for data access abstraction

#### Gang of Four Design Patterns
**ALL 23 GoF patterns are used throughout the platform** - See [Design-Patterns.md](../docs/Design-Patterns.md) for complete implementation guide.

**Creational Patterns:**
- **Abstract Factory**: Payment gateways, Notification channels
- **Builder**: Order creation, Product creation, Search queries
- **Factory Method**: Repository creation, Service instantiation
- **Prototype**: Product duplication, Cart cloning
- **Singleton**: Cache managers, Configuration managers

**Structural Patterns:**
- **Adapter**: External APIs (Google Maps, payment gateways)
- **Bridge**: Notification system (multiple channels/types)
- **Composite**: Category hierarchy, Product bundles
- **Decorator**: Pricing with discounts/taxes, Repository caching
- **Facade**: Checkout process, Order fulfillment
- **Flyweight**: Product attributes, Location data
- **Proxy**: Repository proxies (caching, logging, security)

**Behavioral Patterns:**
- **Chain of Responsibility**: Order validation pipeline
- **Command**: CQRS commands with undo support
- **Interpreter**: Search query parsing, Discount rules
- **Iterator**: Product pagination, Order history
- **Mediator**: CQRS mediator, Event bus
- **Memento**: Order drafts, Cart save-for-later
- **Observer**: Stock alerts, Real-time notifications
- **State**: Order lifecycle, Delivery status
- **Strategy**: Payment methods, Shipping calculations
- **Template Method**: Order processing, Report generation
- **Visitor**: Price/Tax calculation, Invoice generation

#### Project Structure per Service
**Important**: All projects must start with the `R2.ShopNet` prefix.

```
R2.ShopNet.ServiceName/
├── src/
│   ├── R2.ShopNet.ServiceName.API/              # API layer
│   ├── R2.ShopNet.ServiceName.Application/      # CQRS handlers, DTOs
│   ├── R2.ShopNet.ServiceName.Domain/           # Domain models, interfaces
│   └── R2.ShopNet.ServiceName.Infrastructure/   # Data access, external services
├── tests/
│   ├── R2.ShopNet.ServiceName.UnitTests/
│   ├── R2.ShopNet.ServiceName.IntegrationTests/
│   └── R2.ShopNet.ServiceName.ArchitectureTests/
└── R2.ShopNet.ServiceName.sln
```

**Example Project Names**:
- `R2.ShopNet.Catalog` - Product catalog service
- `R2.ShopNet.Cart` - Shopping cart service
- `R2.ShopNet.Orders` - Order management service
- `R2.ShopNet.Payment` - Payment processing service
- `R2.ShopNet.Inventory` - Inventory tracking service
- `R2.ShopNet.Warehouse` - Warehouse operations service
- `R2.ShopNet.Delivery` - Delivery management service
- `R2.ShopNet.Identity` - User and authentication service
- `R2.ShopNet.Authorization` - Roles and permissions service
- `R2.ShopNet.Notifications` - Email/SMS/Push notifications service
- `R2.ShopNet.Search` - Product search service
- `R2.ShopNet.Analytics` - Business analytics service
- `R2.ShopNet.ApiGateway` - API Gateway

**Web Applications**:
- `R2.ShopNet.Web.Shopping` - Customer shopping website
- `R2.ShopNet.Web.Warehouse` - Warehouse management app
- `R2.ShopNet.Web.Delivery` - Delivery driver app (or R2.ShopNet.Mobile.Delivery)
- `R2.ShopNet.Web.Admin` - Admin portal

#### Folder Structure within Application Layer
```
R2.ShopNet.Catalog.Application/
├── Commands/
│   ├── CreateProduct/
│   │   ├── CreateProductCommand.cs
│   │   ├── CreateProductCommandHandler.cs
│   │   └── CreateProductCommandValidator.cs  # Custom validation
│   ├── UpdateProduct/
│   └── DeleteProduct/
├── Queries/
│   ├── GetProduct/
│   │   ├── GetProductByIdQuery.cs
│   │   └── GetProductByIdQueryHandler.cs
│   ├── GetProductList/
│   └── SearchProducts/
├── DTOs/
│   ├── ProductDto.cs
│   ├── ProductListItemDto.cs
│   └── CategoryDto.cs
├── Mappings/
├── Behaviors/  # Pipeline behaviors for cross-cutting concerns
└── Common/
    ├── Interfaces/
    └── Models/
```

### Entity Framework Core Best Practices

#### CQRS with EF Core

**Commands (Write Operations):**
```csharp
// Use full entity tracking for write operations
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly ApplicationDbContext _context;

    public async Task<Result<ProductDto>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = command.Name,
            Price = command.Price,
            CategoryId = command.CategoryId
        };

        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Manual mapping to DTO
        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CategoryId = product.CategoryId
        };

        return Result<ProductDto>.Success(dto);
    }
}
```

**Queries (Read Operations):**
```csharp
// Use AsNoTracking for read-only queries
public class GetProductListQueryHandler : IQueryHandler<GetProductListQuery, Result<List<ProductDto>>>
{
    private readonly ApplicationDbContext _context;

    public async Task<Result<List<ProductDto>>> HandleAsync(GetProductListQuery query, CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Take(100)
            .ToListAsync(cancellationToken);

        // Manual mapping to DTOs
        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name
        }).ToList();

        return Result<List<ProductDto>>.Success(dtos);
    }
}
```

#### Performance Optimization

**Compiled Queries for Frequently Used Reads:**
```csharp
public class ProductQueries
{
    private static readonly Func<ApplicationDbContext, int, Task<Product>> GetProductByIdCompiled =
        EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
            context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .FirstOrDefault(p => p.Id == id));

    public static Task<Product> GetProductByIdAsync(ApplicationDbContext context, int id)
    {
        return GetProductByIdCompiled(context, id);
    }
}

// Usage
var product = await ProductQueries.GetProductByIdAsync(_context, productId);
```

**Split Queries for Complex Includes:**
```csharp
// Use AsSplitQuery to avoid cartesian explosion
var orders = await _context.Orders
    .AsNoTracking()
    .AsSplitQuery()  // Generates separate SQL queries for each Include
    .Include(o => o.Items)
    .Include(o => o.Customer)
    .Include(o => o.ShippingAddress)
    .Where(o => o.CustomerId == customerId)
    .ToListAsync(cancellationToken);
```

**Projection for DTOs:**
```csharp
// Project directly to DTO to avoid loading unnecessary data
var products = await _context.Products
    .AsNoTracking()
    .Where(p => p.IsActive)
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        CategoryName = p.Category.Name
    })
    .ToListAsync(cancellationToken);
```

#### Database Configuration

**DbContext Configuration:**
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableSensitiveDataLogging(false) // Disable in production
            .EnableDetailedErrors(false)       // Disable in production
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Default to no tracking
    }
}
```

**Entity Configuration:**
```csharp
public class ProductEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owned entity
        builder.OwnsOne(p => p.Dimensions, dimensions =>
        {
            dimensions.Property(d => d.Width).HasColumnName("Width");
            dimensions.Property(d => d.Height).HasColumnName("Height");
            dimensions.Property(d => d.Depth).HasColumnName("Depth");
        });
    }
}
```

#### Repository Pattern with EF Core

```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

#### Migrations

**Create Migration:**
```bash
dotnet ef migrations add InitialCreate --project src/R2.ShopNet.Catalog.Infrastructure
```

**Update Database:**
```bash
dotnet ef database update --project src/R2.ShopNet.Catalog.Infrastructure
```

**Generate SQL Script:**
```bash
dotnet ef migrations script --project src/R2.ShopNet.Catalog.Infrastructure --output migration.sql
```

#### Connection Pooling

```csharp
// Startup.cs or Program.cs
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
}, ServiceLifetime.Scoped);

// Connection string with pooling
"Host=localhost;Database=shopnet;Username=postgres;Password=password;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;"
```

### Manual DTO Mapping Patterns

**Philosophy**: Use programmatic/manual mapping instead of external mapper libraries (AutoMapper, Mapster, etc.) for full control, better performance, and easier debugging.

#### Pattern 1: Direct Property Mapping (Simple Entities)

```csharp
// DTO Definition
public record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; }
}

// Command Handler - Single Entity Mapping
public async Task<Result<ProductDto>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
{
    var product = new Product
    {
        Name = command.Name,
        Description = command.Description,
        Price = command.Price,
        CategoryId = command.CategoryId
    };

    await _context.Products.AddAsync(product, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);

    // Manual mapping
    var dto = new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        CategoryId = product.CategoryId,
        CategoryName = null // Not loaded in this context
    };

    return Result<ProductDto>.Success(dto);
}
```

#### Pattern 2: EF Core Projection (Optimal Performance for Queries)

```csharp
// Query Handler - Use Select() projection directly in EF query
public async Task<Result<List<ProductDto>>> HandleAsync(GetProductListQuery query, CancellationToken cancellationToken)
{
    // Best approach: Project directly in the database query
    var dtos = await _context.Products
        .AsNoTracking()
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name // EF handles the join
        })
        .ToListAsync(cancellationToken);

    return Result<List<ProductDto>>.Success(dtos);
}
```

**Benefits of Direct Projection:**
- Only selected columns are retrieved from database (minimal data transfer)
- No unnecessary entity materialization
- Optimal SQL generation by EF Core
- Type-safe at compile time

#### Pattern 3: Static Mapping Methods (Reusable)

```csharp
// Entity Extension or Mapper Class
public static class ProductMappings
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name
        };
    }

    public static List<ProductDto> ToDtoList(this IEnumerable<Product> products)
    {
        return products.Select(p => p.ToDto()).ToList();
    }

    public static ProductDetailDto ToDetailDto(this Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            Variants = product.Variants?.Select(v => new VariantDto
            {
                Id = v.Id,
                Name = v.Name,
                Sku = v.Sku
            }).ToList() ?? new List<VariantDto>()
        };
    }
}

// Usage in handlers
var product = await _context.Products
    .Include(p => p.Category)
    .Include(p => p.Variants)
    .FirstOrDefaultAsync(p => p.Id == query.ProductId, cancellationToken);

var dto = product.ToDetailDto();
```

#### Pattern 4: Mapping Complex Nested Objects

```csharp
public static class OrderMappings
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.FullName,
            Status = order.Status.ToString(),
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            Items = order.Items?.Select(item => new OrderItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.Quantity * item.UnitPrice
            }).ToList() ?? new List<OrderItemDto>()
        };
    }
}
```

#### Pattern 5: Mapping with Business Logic

```csharp
public static class ProductMappings
{
    public static ProductDto ToDto(this Product product, decimal? userDiscountRate = null)
    {
        var finalPrice = product.Price;

        // Apply discount if provided
        if (userDiscountRate.HasValue && userDiscountRate.Value > 0)
        {
            finalPrice = product.Price * (1 - userDiscountRate.Value);
        }

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            OriginalPrice = product.Price,
            FinalPrice = finalPrice,
            HasDiscount = userDiscountRate.HasValue && userDiscountRate.Value > 0,
            StockStatus = product.Stock > 0 ? "In Stock" : "Out of Stock"
        };
    }
}
```

#### Best Practices for Manual Mapping

1. **Prefer EF Core Projection for Queries**: Use `Select()` directly in queries to project to DTOs
2. **Use Extension Methods for Reusability**: Create `ToDto()` extension methods for common mappings
3. **Keep DTOs Immutable**: Use `record` types or `init` properties
4. **Null Safety**: Always handle nullable navigation properties with `?.` and `??` operators
5. **Avoid Over-fetching**: Only map properties that are actually needed in the DTO
6. **Complex Mappings**: For very complex scenarios, create dedicated mapper classes
7. **Testing**: Manual mapping is easier to test and debug than reflection-based mappers

#### Anti-Patterns to Avoid

❌ **Don't fetch full entity then map in memory (unless necessary)**:
```csharp
// BAD - Fetches all columns
var products = await _context.Products.ToListAsync();
var dtos = products.Select(p => new ProductDto { ... });
```

✅ **Do project directly in the query**:
```csharp
// GOOD - Only fetches needed columns
var dtos = await _context.Products
    .Select(p => new ProductDto { Id = p.Id, Name = p.Name })
    .ToListAsync();
```

❌ **Don't use generic mapping methods that rely on reflection**
✅ **Do use explicit, type-safe mapping**

### Angular 20 Frontend Architecture

**Philosophy**: Use Angular 20's latest stable features including Signals, Standalone Components, and Zoneless change detection for maximum performance and developer experience.

#### Project Structure for Angular Web Applications

```
R2.ShopNet.Web.Shopping/           (Customer shopping site)
R2.ShopNet.Web.Warehouse/          (Warehouse management)
R2.ShopNet.Web.Delivery/           (Driver delivery app)
R2.ShopNet.Web.Admin/              (Admin portal)

Each Angular app follows this structure:
src/
├── app/
│   ├── core/                      (Singleton services, guards, interceptors)
│   │   ├── services/
│   │   ├── guards/
│   │   ├── interceptors/
│   │   └── models/
│   ├── features/                  (Feature modules/components)
│   │   ├── products/
│   │   ├── cart/
│   │   ├── orders/
│   │   └── auth/
│   ├── shared/                    (Shared components, directives, pipes)
│   │   ├── components/
│   │   ├── directives/
│   │   └── pipes/
│   ├── app.component.ts           (Root component)
│   └── app.routes.ts              (Route configuration)
├── assets/
├── environments/
└── main.ts
```

#### Angular 20 Key Features Implementation

**1. Standalone Components (No NgModules)**

```typescript
// app.component.ts - Root component
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './shared/components/header/header.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent],
  template: `
    <app-header />
    <router-outlet />
  `
})
export class AppComponent {}

// main.ts - Bootstrap with standalone
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { authInterceptor } from './app/core/interceptors/auth.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideZonelessChangeDetection(),  // Zoneless (stable in v20.2)
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
});
```

**2. Angular Signals for State Management (Stable in v20)**

```typescript
// Product service with Signals
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Product {
  id: number;
  name: string;
  price: number;
  stock: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  // Signals for reactive state
  private productsSignal = signal<Product[]>([]);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  // Public readonly signals
  readonly products = this.productsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  // Computed signals
  readonly inStockProducts = computed(() =>
    this.productsSignal().filter(p => p.stock > 0)
  );

  readonly totalProducts = computed(() =>
    this.productsSignal().length
  );

  async loadProducts(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    try {
      const products = await firstValueFrom(
        this.http.get<Product[]>('/api/products')
      );
      this.productsSignal.set(products);
    } catch (error) {
      this.errorSignal.set('Failed to load products');
    } finally {
      this.loadingSignal.set(false);
    }
  }

  addProduct(product: Product): void {
    this.productsSignal.update(products => [...products, product]);
  }

  updateProduct(id: number, updates: Partial<Product>): void {
    this.productsSignal.update(products =>
      products.map(p => p.id === id ? { ...p, ...updates } : p)
    );
  }
}
```

**3. Signal-based Components**

```typescript
// Product list component using Signals
import { Component, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductService } from './product.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="product-list">
      @if (productService.loading()) {
        <div class="loading">Loading products...</div>
      } @else if (productService.error()) {
        <div class="error">{{ productService.error() }}</div>
      } @else {
        <div class="product-count">
          Total: {{ productService.totalProducts() }} |
          In Stock: {{ productService.inStockProducts().length }}
        </div>

        @for (product of productService.products(); track product.id) {
          <div class="product-card">
            <h3>{{ product.name }}</h3>
            <p>Price: {{ product.price | currency }}</p>
            <p>Stock: {{ product.stock }}</p>
          </div>
        }
      }
    </div>
  `
})
export class ProductListComponent {
  protected readonly productService = inject(ProductService);

  constructor() {
    // Effect runs when signals change
    effect(() => {
      console.log(`Product count: ${this.productService.totalProducts()}`);
    });

    // Load products on init
    this.productService.loadProducts();
  }
}
```

**4. Signal Inputs (Stable in v20)**

```typescript
// Product detail component with signal inputs
import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  template: `
    <div class="product-detail">
      <h2>{{ product().name }}</h2>
      <p>{{ displayPrice() }}</p>
      <span [class.in-stock]="isInStock()" [class.out-of-stock]="!isInStock()">
        {{ stockStatus() }}
      </span>
    </div>
  `
})
export class ProductDetailComponent {
  // Signal inputs (type-safe, reactive)
  product = input.required<Product>();
  showTax = input<boolean>(false);

  // Computed based on inputs
  displayPrice = computed(() => {
    const price = this.product().price;
    return this.showTax() ? price * 1.2 : price;
  });

  isInStock = computed(() => this.product().stock > 0);

  stockStatus = computed(() =>
    this.isInStock() ? 'In Stock' : 'Out of Stock'
  );
}

// Usage
// <app-product-detail [product]="currentProduct()" [showTax]="true" />
```

**5. HTTP Client with Signals**

```typescript
// API service with Signal-based responses
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/orders';

  // Convert Observable to Signal
  orders$ = this.http.get<Order[]>(this.baseUrl);
  orders = toSignal(this.orders$, { initialValue: [] });

  // Async operations returning Promises
  async createOrder(order: CreateOrderDto): Promise<Order> {
    return firstValueFrom(
      this.http.post<Order>(this.baseUrl, order)
    );
  }

  async getOrderById(id: number): Promise<Order> {
    return firstValueFrom(
      this.http.get<Order>(`${this.baseUrl}/${id}`)
    );
  }

  async updateOrderStatus(id: number, status: string): Promise<Order> {
    return firstValueFrom(
      this.http.patch<Order>(`${this.baseUrl}/${id}/status`, { status })
    );
  }
}
```

**6. Routing with Signal-based Guards**

```typescript
// app.routes.ts
import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './core/services/auth.service';

export const routes: Routes = [
  {
    path: 'products',
    loadComponent: () => import('./features/products/product-list.component')
      .then(m => m.ProductListComponent)
  },
  {
    path: 'admin',
    canActivate: [() => inject(AuthService).isAdmin()],
    loadComponent: () => import('./features/admin/admin.component')
      .then(m => m.AdminComponent)
  },
  {
    path: 'orders',
    canActivate: [() => inject(AuthService).isAuthenticated()],
    loadChildren: () => import('./features/orders/orders.routes')
      .then(m => m.ORDERS_ROUTES)
  }
];

// Auth guard using signals
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly isAuthenticatedSignal = signal<boolean>(false);
  private readonly userRoleSignal = signal<string>('guest');

  readonly isAuthenticated = this.isAuthenticatedSignal.asReadonly();
  readonly isAdmin = computed(() => this.userRoleSignal() === 'admin');
  readonly isWarehouseManager = computed(() =>
    this.userRoleSignal() === 'warehouse_manager'
  );
}
```

**7. Form Handling with Signals**

```typescript
// Reactive forms with Signal validation
import { Component, signal, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()">
      <input formControlName="name" placeholder="Product name">
      @if (nameErrors()) {
        <span class="error">{{ nameErrors() }}</span>
      }

      <input type="number" formControlName="price" placeholder="Price">
      @if (priceErrors()) {
        <span class="error">{{ priceErrors() }}</span>
      }

      <button type="submit" [disabled]="!isValid()">
        Save Product
      </button>
    </form>
  `
})
export class ProductFormComponent {
  form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    price: new FormControl(0, [Validators.required, Validators.min(0)])
  });

  // Convert form state to signals
  formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });
  formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });

  isValid = computed(() => this.formStatus() === 'VALID');

  nameErrors = computed(() => {
    const control = this.form.get('name');
    if (control?.hasError('required')) return 'Name is required';
    if (control?.hasError('minlength')) return 'Name must be at least 3 characters';
    return null;
  });

  priceErrors = computed(() => {
    const control = this.form.get('price');
    if (control?.hasError('required')) return 'Price is required';
    if (control?.hasError('min')) return 'Price must be positive';
    return null;
  });

  onSubmit(): void {
    if (this.form.valid) {
      console.log('Form submitted:', this.form.value);
    }
  }
}
```

#### Angular Best Practices for R2.ShopNet

1. **Use Standalone Components**: No NgModules, all components are standalone
2. **Leverage Signals**: Use Angular Signals for all reactive state (no NgRx needed)
3. **Zoneless Change Detection**: Enable for better performance (stable in v20.2)
4. **Signal Inputs**: Use `input()` and `output()` for component communication
5. **Lazy Loading**: Use `loadComponent` and `loadChildren` for route-based code splitting
6. **SSR with Route-Level Render Modes**: Enable SSR for SEO and initial load performance
7. **Type Safety**: Strict TypeScript configuration with no `any` types
8. **Immutability**: Always use immutable patterns with signals (`.update()`, `.set()`)
9. **Computed Values**: Use `computed()` instead of manual derived state
10. **Effects**: Use `effect()` for side effects triggered by signal changes

#### Angular Testing

```typescript
// Component testing with Signals
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ProductListComponent } from './product-list.component';
import { ProductService } from './product.service';

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let fixture: ComponentFixture<ProductListComponent>;
  let mockProductService: jasmine.SpyObj<ProductService>;

  beforeEach(async () => {
    // Create mock service with signals
    mockProductService = jasmine.createSpyObj('ProductService', ['loadProducts'], {
      products: signal<Product[]>([]),
      loading: signal(false),
      error: signal(null),
      totalProducts: computed(() => 0)
    });

    await TestBed.configureTestingModule({
      imports: [ProductListComponent],
      providers: [
        { provide: ProductService, useValue: mockProductService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;
  });

  it('should display products', () => {
    const products = [
      { id: 1, name: 'Product 1', price: 100, stock: 10 }
    ];

    mockProductService.products = signal(products);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('.product-card')).toBeTruthy();
  });
});
```

### Testing Strategy

#### Unit Tests
- Minimum 80% code coverage
- Test business logic in isolation
- Use xUnit as test framework
- NSubstitute for mocking dependencies (MIT license, simpler than Moq)
- Custom assertion helpers or xUnit's Assert class
- Arrange-Act-Assert pattern

#### Integration Tests
- Use Testcontainers for database/message queue
- Test API endpoints end-to-end
- WebApplicationFactory for in-memory testing
- Separate integration test database
- Clean database state between tests

#### Architecture Tests
- Use NetArchTest.Rules
- Enforce layering rules (Domain shouldn't reference Infrastructure)
- Verify naming conventions
- Ensure CQRS separation

#### Load Tests
- Use k6 or JMeter
- Test under 10,000+ concurrent users
- Validate response times < 200ms (p95)
- Test auto-scaling behavior

**Test Naming Convention:**
```csharp
[Fact]
public async Task HandleAsync_WhenValidCommand_ShouldCreateContent()
{
    // Arrange
    var command = new CreateContentCommand { /* ... */ };

    // Act
    var result = await _handler.HandleAsync(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
}
```

### Consul Service Discovery & Configuration

**Philosophy**: Use Consul for dynamic service discovery, health checking, and distributed configuration management across all microservices.

#### Consul Architecture in R2.ShopNet

```
┌─────────────────────────────────────────────────────────────┐
│                    Consul Cluster (3 nodes)                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Consul      │  │  Consul      │  │  Consul      │     │
│  │  Server 1    │  │  Server 2    │  │  Server 3    │     │
│  │  (Leader)    │  │  (Follower)  │  │  (Follower)  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
         │                    │                    │
         └────────────────────┴────────────────────┘
                              │
         ┌────────────────────┴────────────────────┐
         │                                         │
    ┌────▼─────┐                            ┌─────▼────┐
    │  YARP    │                            │ Services │
    │ Gateway  │                            │  (13)    │
    │ (Consul  │                            │  with    │
    │  Client) │                            │ Consul   │
    └──────────┘                            │ Clients  │
                                            └──────────┘
```

#### Consul Installation & Configuration

**Docker Compose Setup:**
```yaml
# docker-compose.yml
services:
  consul-server:
    image: hashicorp/consul:1.19
    container_name: consul-server
    restart: unless-stopped
    ports:
      - "8500:8500"    # HTTP API & UI
      - "8600:8600/udp" # DNS
      - "8300:8300"    # Server RPC
      - "8301:8301"    # Serf LAN
      - "8302:8302"    # Serf WAN
    environment:
      CONSUL_BIND_INTERFACE: eth0
    command: >
      agent -server
      -ui
      -bootstrap-expect=1
      -client=0.0.0.0
      -bind=0.0.0.0
      -datacenter=shopnet-dc1
      -node=consul-server-1
    volumes:
      - consul_data:/consul/data
      - consul_config:/consul/config
    networks:
      - shopnet-network
    healthcheck:
      test: ["CMD", "consul", "members"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  consul_data:
  consul_config:

networks:
  shopnet-network:
    driver: bridge
```

**Consul Configuration File (config.json):**
```json
{
  "datacenter": "shopnet-dc1",
  "data_dir": "/consul/data",
  "log_level": "INFO",
  "server": true,
  "ui": true,
  "bootstrap_expect": 1,
  "addresses": {
    "http": "0.0.0.0"
  },
  "ports": {
    "http": 8500,
    "dns": 8600
  },
  "enable_script_checks": false,
  "enable_local_script_checks": true,
  "services": []
}
```

#### Service Registration with Consul

**Install Consul NuGet Package:**
```bash
dotnet add package Consul --version 1.7.14
```

**Service Registration Implementation:**
```csharp
// R2.ShopNet.Framework.ServiceDiscovery/ConsulServiceRegistration.cs
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ConsulServiceRegistration : IHostedService
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsulServiceRegistration> _logger;
    private string _serviceId;

    public ConsulServiceRegistration(
        IConsulClient consulClient,
        IConfiguration configuration,
        ILogger<ConsulServiceRegistration> logger)
    {
        _consulClient = consulClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceName = _configuration["ServiceName"] ?? "unknown-service";
        var serviceHost = _configuration["ServiceHost"] ?? "localhost";
        var servicePort = int.Parse(_configuration["ServicePort"] ?? "5000");

        _serviceId = $"{serviceName}-{Guid.NewGuid()}";

        var registration = new AgentServiceRegistration
        {
            ID = _serviceId,
            Name = serviceName,
            Address = serviceHost,
            Port = servicePort,
            Tags = new[]
            {
                "api",
                "microservice",
                $"version-{_configuration["ServiceVersion"] ?? "1.0"}"
            },
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{serviceHost}:{servicePort}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
        _logger.LogInformation(
            "Service {ServiceId} registered with Consul at {ServiceHost}:{ServicePort}",
            _serviceId, serviceHost, servicePort);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
        _logger.LogInformation("Service {ServiceId} deregistered from Consul", _serviceId);
    }
}
```

**Program.cs Configuration:**
```csharp
// Program.cs in each microservice
using Consul;

var builder = WebApplication.CreateBuilder(args);

// Add Consul client
builder.Services.AddSingleton<IConsulClient>(p =>
{
    var consulConfig = new ConsulClientConfiguration
    {
        Address = new Uri(builder.Configuration["Consul:Address"] ?? "http://localhost:8500")
    };
    return new ConsulClient(consulConfig);
});

// Add service registration
builder.Services.AddHostedService<ConsulServiceRegistration>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

**appsettings.json Configuration:**
```json
{
  "Consul": {
    "Address": "http://consul-server:8500"
  },
  "ServiceName": "catalog-service",
  "ServiceHost": "catalog-service",
  "ServicePort": "5001",
  "ServiceVersion": "1.0.0"
}
```

#### Service Discovery for Inter-Service Communication

**Service Discovery Service:**
```csharp
// R2.ShopNet.Framework.ServiceDiscovery/ConsulServiceDiscovery.cs
using Consul;

public interface IServiceDiscovery
{
    Task<Uri> GetServiceUriAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<List<Uri>> GetAllServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default);
}

public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConsulClient consulClient, ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task<Uri> GetServiceUriAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Health.Service(serviceName, tag: null, passingOnly: true, cancellationToken);

        if (services.Response == null || services.Response.Length == 0)
        {
            _logger.LogWarning("No healthy instances found for service {ServiceName}", serviceName);
            throw new InvalidOperationException($"No healthy instances found for service {serviceName}");
        }

        // Simple round-robin (in production, use more sophisticated load balancing)
        var service = services.Response[Random.Shared.Next(services.Response.Length)];
        var uri = new Uri($"http://{service.Service.Address}:{service.Service.Port}");

        _logger.LogDebug("Resolved {ServiceName} to {Uri}", serviceName, uri);
        return uri;
    }

    public async Task<List<Uri>> GetAllServiceInstancesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await _consulClient.Health.Service(serviceName, tag: null, passingOnly: true, cancellationToken);

        return services.Response
            .Select(s => new Uri($"http://{s.Service.Address}:{s.Service.Port}"))
            .ToList();
    }
}
```

**Using Service Discovery in HTTP Clients:**
```csharp
// Example: Catalog service calling Inventory service
public class InventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceDiscovery _serviceDiscovery;

    public InventoryClient(HttpClient httpClient, IServiceDiscovery serviceDiscovery)
    {
        _httpClient = httpClient;
        _serviceDiscovery = serviceDiscovery;
    }

    public async Task<InventoryDto> GetInventoryAsync(int productId, CancellationToken cancellationToken)
    {
        // Dynamically resolve inventory service URL from Consul
        var serviceUri = await _serviceDiscovery.GetServiceUriAsync("inventory-service", cancellationToken);
        var requestUri = new Uri(serviceUri, $"/api/inventory/product/{productId}");

        var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<InventoryDto>(cancellationToken);
    }
}

// Register in DI container
builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
builder.Services.AddHttpClient<InventoryClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

#### YARP API Gateway with Consul Integration

**Install YARP Consul Extension:**
```bash
dotnet add package Yarp.ReverseProxy.Consul
```

**YARP Configuration with Consul:**
```csharp
// R2.ShopNet.ApiGateway/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConsulServiceDiscovery();

var app = builder.Build();

app.MapReverseProxy();
app.Run();
```

**appsettings.json for YARP:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/api/products/{**catch-all}"
        }
      },
      "inventory-route": {
        "ClusterId": "inventory-cluster",
        "Match": {
          "Path": "/api/inventory/{**catch-all}"
        }
      },
      "orders-route": {
        "ClusterId": "orders-cluster",
        "Match": {
          "Path": "/api/orders/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "consul": {
            "Address": "consul://catalog-service"
          }
        },
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:05",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          }
        }
      },
      "inventory-cluster": {
        "Destinations": {
          "consul": {
            "Address": "consul://inventory-service"
          }
        },
        "LoadBalancingPolicy": "RoundRobin"
      },
      "orders-cluster": {
        "Destinations": {
          "consul": {
            "Address": "consul://orders-service"
          }
        },
        "LoadBalancingPolicy": "LeastRequests"
      }
    }
  },
  "Consul": {
    "Address": "http://consul-server:8500"
  }
}
```

#### Consul KV Store for Configuration Management

**Store Configuration in Consul:**
```bash
# Using Consul CLI
consul kv put config/shopnet/catalog-service/database/connection-string "Host=postgres;Database=catalog;..."
consul kv put config/shopnet/catalog-service/redis/connection-string "redis:6379"
consul kv put config/shopnet/global/max-page-size "100"
consul kv put config/shopnet/global/cache-expiration "300"
```

**Read Configuration from Consul:**
```csharp
// R2.ShopNet.Framework.Configuration/ConsulConfigurationProvider.cs
using Consul;

public class ConsulConfigurationProvider
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;

    public ConsulConfigurationProvider(IConsulClient consulClient, string serviceName)
    {
        _consulClient = consulClient;
        _serviceName = serviceName;
    }

    public async Task<string> GetConfigAsync(string key, CancellationToken cancellationToken = default)
    {
        var consulKey = $"config/shopnet/{_serviceName}/{key}";
        var result = await _consulClient.KV.Get(consulKey, cancellationToken);

        if (result.Response == null)
        {
            throw new KeyNotFoundException($"Configuration key {consulKey} not found in Consul");
        }

        return Encoding.UTF8.GetString(result.Response.Value);
    }

    public async Task<Dictionary<string, string>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        var consulKeyPrefix = $"config/shopnet/{_serviceName}/";
        var result = await _consulClient.KV.List(consulKeyPrefix, cancellationToken);

        if (result.Response == null)
        {
            return new Dictionary<string, string>();
        }

        return result.Response.ToDictionary(
            kvp => kvp.Key.Replace(consulKeyPrefix, ""),
            kvp => Encoding.UTF8.GetString(kvp.Value)
        );
    }
}
```

**Watch for Configuration Changes:**
```csharp
// Background service to watch for configuration changes
public class ConsulConfigurationWatcher : BackgroundService
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulConfigurationWatcher> _logger;
    private ulong _lastIndex = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queryOptions = new QueryOptions { WaitIndex = _lastIndex };
                var result = await _consulClient.KV.List("config/shopnet/", queryOptions, stoppingToken);

                if (result.LastIndex > _lastIndex)
                {
                    _lastIndex = result.LastIndex;
                    _logger.LogInformation("Configuration changed, reloading...");
                    // Trigger configuration reload
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error watching Consul configuration");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
```

#### Consul Health Checks

**Custom Health Check Implementation:**
```csharp
// Custom health check that reports to Consul
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck("redis", () =>
    {
        // Check Redis connection
        return HealthCheckResult.Healthy();
    })
    .AddCheck("rabbitmq", () =>
    {
        // Check RabbitMQ connection
        return HealthCheckResult.Healthy();
    });
```

#### Consul Benefits for R2.ShopNet

**1. Dynamic Service Discovery**
- Services automatically discover each other without hardcoded URLs
- New service instances automatically registered
- Failed instances automatically deregistered
- Zero-downtime deployments with rolling updates

**2. Health Checking**
- Automatic health monitoring of all services
- Failed services removed from load balancing pool
- HTTP, TCP, and script-based health checks
- Configurable check intervals and timeouts

**3. Load Balancing**
- Client-side load balancing via service discovery
- Multiple load balancing strategies (round-robin, least connections)
- No single point of failure

**4. Configuration Management**
- Centralized configuration storage
- Environment-specific configurations
- Real-time configuration updates without redeployment
- Version control for configurations

**5. Service Mesh Ready**
- Foundation for future Consul Connect integration
- Secure service-to-service communication
- Mutual TLS between services

#### Best Practices

1. **Always use health checks** - Every service must expose `/health` endpoint
2. **Tag services appropriately** - Use tags for versioning, environments, features
3. **Handle discovery failures** - Implement fallbacks when services are unavailable
4. **Cache service lookups** - Reduce Consul query load with short-lived caching
5. **Monitor Consul cluster** - Ensure Consul itself is healthy and available
6. **Use Consul intentions** - Define which services can communicate (security)
7. **Implement circuit breakers** - Use Polly with service discovery for resilience
8. **Test failure scenarios** - Simulate Consul unavailability and service failures

### Git Workflow

#### Branching Strategy
- **main**: Production-ready code, protected branch
- **develop**: Integration branch for features
- **feature/[ticket-id]-[short-description]**: Feature branches
- **bugfix/[ticket-id]-[short-description]**: Bug fix branches
- **hotfix/[ticket-id]-[short-description]**: Production hotfixes

#### Commit Conventions
Follow Conventional Commits specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements

**Example:**
```
feat(catalog): add product variant support

Implement product variants to support multiple sizes and colors.
Each variant has its own SKU and inventory tracking.

Closes #123
```

#### Pull Request Requirements
- PR must be linked to a ticket/issue
- Minimum 1 approval required
- All CI checks must pass
- Code coverage must not decrease
- Update relevant documentation
- Add/update tests for new features

## Domain Context

### E-Commerce & Logistics Domain

#### Core Entities

**Catalog Domain:**
- **Product**: Product information (name, description, price, images)
- **Category**: Product categories and hierarchies
- **Brand**: Product brands/manufacturers
- **ProductVariant**: Product variations (size, color, etc.)
- **ProductAttribute**: Custom product attributes

**Shopping Domain:**
- **Cart**: Customer shopping cart
- **CartItem**: Items in the shopping cart
- **Wishlist**: Customer saved items

**Order Domain:**
- **Order**: Customer purchase order
- **OrderItem**: Line items in an order
- **OrderStatus**: Order lifecycle state
- **Invoice**: Order invoice and payment details
- **Shipment**: Shipping information

**Inventory Domain:**
- **Stock**: Product stock levels by warehouse
- **StockReservation**: Reserved stock for pending orders
- **StockMovement**: Inventory adjustments and transfers
- **Warehouse**: Warehouse location and details

**Delivery Domain:**
- **Delivery**: Delivery assignment and tracking
- **DeliveryRoute**: Optimized delivery route
- **Driver**: Delivery driver information
- **DeliveryProof**: Signature and photo proof

**User & Security Domain:**
- **User**: System users (customers, staff, drivers, admins)
- **Role**: User roles (Customer, WarehouseStaff, Driver, Admin, etc.)
- **Permission**: Granular permissions
- **AuditLog**: System audit trail

#### Order Lifecycle States
1. **Pending**: Order created, awaiting payment
2. **PaymentConfirmed**: Payment received
3. **Processing**: Being picked in warehouse
4. **Packed**: Ready for shipment
5. **Shipped**: Out for delivery
6. **Delivered**: Successfully delivered
7. **Cancelled**: Order cancelled
8. **Returned**: Customer returned the order

#### Event Flow Examples

**Order Placement:**
```
Cart → Checkout → Payment Processed → Order Created Event →
Inventory Reserved → Warehouse Notified → Picking Started →
Order Packed → Shipment Created → Delivery Assigned →
Delivery In Progress → Delivery Completed → Customer Notified
```

**Inventory Update:**
```
Stock Received → Inventory Updated Event → Catalog Service Updates Availability →
Cache Invalidated → Search Index Updated → Low Stock Alert (if applicable)
```

**Delivery Tracking:**
```
Order Shipped → Delivery Assigned → Driver Accepted →
En Route (GPS Updates) → Arrived at Location →
Delivery Confirmed (Signature + Photo) → Customer Notified
```

### API Design Principles
- RESTful conventions for resource operations
- Versioning via URL path (`/api/v1/products`)
- Pagination for list endpoints (cursor-based preferred)
- Standard error responses with problem details (RFC 7807)
- HATEOAS links for API discoverability
- Idempotency keys for critical operations (orders, payments)
- Rate limiting per user/IP
- API documentation with Swagger/OpenAPI

## Important Constraints

### Technical Constraints
- All services must be containerized with Docker
- Maximum API response time: 200ms (p95)
- Database queries must complete within 100ms
- No blocking operations on API threads
- All external calls must have timeout and retry policies (Polly)
- Secrets must be stored in environment variables or HashiCorp Vault (self-hosted)
- No hardcoded connection strings or credentials
- Use Docker secrets for sensitive configuration in containerized environments

### Security Constraints
- OWASP Top 10 compliance mandatory
- All data encrypted at rest (database encryption) and in transit (TLS 1.3+)
- Authentication required for all write operations
- Role-Based Access Control (RBAC) enforced
- Content-level and field-level permissions
- Audit logging for all state changes
- Regular security scanning (Dependabot, Snyk, Trivy for containers)
- Network segmentation and firewall rules for internal services
- Regular backup and disaster recovery procedures

### Performance Constraints
- Support 10,000+ concurrent users
- Handle 1M+ content items per tenant
- 99.9% uptime SLA
- Zero-downtime deployments via rolling updates
- Database backup RTO: 4 hours, RPO: 15 minutes (automated local backups)
- Nginx or Varnish for static asset caching and reverse proxy
- Load balancer (HAProxy/Nginx) for high availability

### Compliance Constraints
- GDPR compliance for EU users
  - Right to access personal data
  - Right to be forgotten
  - Data portability
  - Consent management
- Data residency: all data stored on-premises/local servers
- Audit trail retention: minimum 7 years (local storage)
- PII data must be anonymized in logs
- Regular compliance audits and security assessments

### Business Constraints
- Must support B2C and B2B scenarios
- Multiple payment methods (credit card, PayPal, bank transfer, COD)
- Multi-currency support
- Multi-language support
- Tax calculation per region
- Inventory accuracy > 99%
- Order fulfillment time < 24 hours
- On-time delivery rate > 95%

## External Dependencies

### Self-Hosted Infrastructure
- **Docker / Docker Compose**: Container orchestration for development
- **Kubernetes (k3s/MicroK8s)**: Optional container orchestration for production
- **PostgreSQL**: Self-hosted relational database (containerized)
- **Redis**: Self-hosted distributed cache (containerized)
- **Elasticsearch**: Self-hosted full-text search engine (containerized)
- **RabbitMQ**: Self-hosted message queue (containerized)
- **MinIO**: Self-hosted S3-compatible object storage (containerized)
- **HashiCorp Vault**: Self-hosted secrets management (optional)
- **Nginx/HAProxy**: Load balancer and reverse proxy
- **Varnish Cache**: HTTP accelerator for static content (optional)

### Optional Third-Party Services (If Needed)
- **SMTP Server**: Self-hosted email server (Postfix) or external SMTP relay
- **SMS Gateway**: Optional third-party integration (can be disabled)
- **Payment Processing**: Optional integration (Stripe/PayPal) if billing required
- **GitLab Self-Hosted**: CI/CD pipelines (alternative to GitHub Actions)
- **Jenkins**: Self-hosted CI/CD (alternative)
- **SonarQube**: Self-hosted code quality analysis

### Development Tools
- **Docker Desktop / Podman**: Local containerization
- **Visual Studio 2022 / JetBrains Rider**: IDE
- **.NET Aspire Dashboard**: Local development observability
- **Postman / Insomnia / Bruno**: API testing
- **pgAdmin / DBeaver**: PostgreSQL database management
- **RedisInsight / Redis Commander**: Redis management
- **k6 / Apache JMeter / Gatling**: Load testing
- **Portainer**: Docker container management UI (optional)

### Monitoring & Observability Stack (Self-Hosted)
- **OpenTelemetry**: Distributed tracing
- **Prometheus**: Metrics collection (self-hosted)
- **Grafana**: Metrics visualization (self-hosted)
- **Loki**: Log aggregation (self-hosted, pairs with Grafana)
- **Jaeger**: Distributed tracing UI (self-hosted)
- **Seq**: Structured logging (self-hosted, free for single user)
- **Alertmanager**: Alert routing and management (pairs with Prometheus)
- **Uptime Kuma**: Self-hosted uptime monitoring

### Package Dependencies (Key NuGet Packages)

#### Core Framework
- `Aspire.Hosting.AppHost` (9.5.1) - MIT license
- **Custom CQRS** - Built in-house (no external package)
- **Custom Validation** - DataAnnotations + in-house framework
- **Manual DTO Mapping** - Programmatic mapping (no external mapper)
- `Serilog.AspNetCore` (8.x) - Apache 2.0 license
- `Polly` (8.x) - BSD license

#### Entity Framework Core (Complete Data Access)
- `Microsoft.EntityFrameworkCore` (9.x) - MIT license
- `Microsoft.EntityFrameworkCore.PostgreSQL` (9.x) - MIT license (PostgreSQL provider)
- `Microsoft.EntityFrameworkCore.Design` (9.x) - MIT license (for migrations)
- `Microsoft.EntityFrameworkCore.Tools` (9.x) - MIT license (for CLI)
- `Microsoft.EntityFrameworkCore.Analyzers` (9.x) - MIT license (code analysis)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (9.x) - PostgreSQL License (permissive)

#### Infrastructure & Service Discovery
- `Consul` (1.7.14) - Apache 2.0 license (service discovery & configuration)
- `Yarp.ReverseProxy.Consul` (2.x) - MIT license (API Gateway with Consul)
- `StackExchange.Redis` (2.x) - MIT license
- `Elastic.Clients.Elasticsearch` (8.x) - Apache 2.0 license
- `RabbitMQ.Client` (6.x) - Apache 2.0/MPL license
- `Swashbuckle.AspNetCore` (6.x) - MIT license (OpenAPI/Swagger)
- `OpenIddict` (5.x) - Apache 2.0 license
- `SkiaSharp` (2.x) - MIT license (image processing)

#### Testing
- `xUnit` (2.x) - Apache 2.0 license
- `NSubstitute` (5.x) - MIT license
- `Testcontainers` (3.x) - MIT license
- `Microsoft.EntityFrameworkCore.InMemory` (9.x) - MIT license (for testing)
- `NetArchTest.Rules` (1.x) - MIT license (architecture tests)

#### Angular Frontend Packages (npm)

**Core Framework:**
- `@angular/core` (20.x) - MIT license
- `@angular/common` (20.x) - MIT license
- `@angular/platform-browser` (20.x) - MIT license
- `@angular/platform-browser-dynamic` (20.x) - MIT license
- `@angular/router` (20.x) - MIT license
- `@angular/forms` (20.x) - MIT license
- `typescript` (5.7+) - Apache 2.0 license
- `rxjs` (7.x) - Apache 2.0 license
- `zone.js` (0.15.x) - MIT license (optional with zoneless)

**UI Components (Choose one):**
- `@angular/material` (20.x) - MIT license (Material Design)
- `tailwindcss` (3.x) - MIT license (Utility-first CSS)
- `@angular/cdk` (20.x) - MIT license (Component Dev Kit)

**HTTP & State:**
- `@angular/common/http` (20.x) - MIT license (included in @angular/common)
- Angular Signals (built-in, no external state management library needed)

**Development Tools:**
- `@angular/cli` (20.x) - MIT license
- `@angular-devkit/build-angular` (20.x) - MIT license
- `jasmine-core` (5.x) - MIT license (testing)
- `karma` (6.x) - MIT license (test runner)
- `@angular/platform-server` (20.x) - MIT license (for SSR)

**Utilities:**
- `date-fns` (4.x) - MIT license (date manipulation)
- `lodash-es` (4.x) - MIT license (utilities, tree-shakeable)

## Service Communication Patterns

### Synchronous Communication
- HTTP/REST for real-time requests
- gRPC for internal service-to-service calls (optional)
- Timeout: 5 seconds default
- Circuit breaker after 5 consecutive failures
- Retry: 3 attempts with exponential backoff

### Asynchronous Communication
- Message queue (RabbitMQ/Service Bus) for events
- At-least-once delivery guarantee
- Idempotent message handlers
- Dead-letter queue for failed messages
- Message retention: 7 days

### Event Naming Convention
```
{Domain}.{Entity}.{Action}
Example: Content.ContentItem.Published
```

## Deployment Strategy

### Environments
- **Local**: .NET Aspire orchestrated development with Docker Compose
- **Development**: Shared dev environment (on-premises server)
- **Staging**: Production-like environment (on-premises)
- **Production**: Live environment with rolling updates or blue-green deployment

### Infrastructure Setup
- **Hardware Requirements** (minimum for production):
  - CPU: 8+ cores
  - RAM: 32GB+
  - Storage: 500GB+ SSD (RAID configuration recommended)
  - Network: 1Gbps+ connectivity
- **Containerization**: All services run in Docker containers
- **Orchestration**: Docker Compose (simple) or Kubernetes/k3s (advanced)
- **Load Balancing**: Nginx or HAProxy in front of service instances
- **High Availability**: Multiple instances of critical services

### CI/CD Pipeline (Self-Hosted)
1. Code commit → GitLab/Gitea (self-hosted) or GitHub
2. Trigger Jenkins/GitLab CI pipeline
3. Run unit tests
4. Run integration tests
5. Run security scan (Trivy for containers, SonarQube)
6. Build Docker images
7. Push to local Docker registry (Harbor or GitLab Registry)
8. Deploy to target environment via SSH/Ansible
9. Run smoke tests
10. Health check verification

### Deployment Methods
- **Docker Compose**: For single-server or small deployments
- **Kubernetes (k3s)**: For multi-server, high-availability setups
- **Ansible Playbooks**: Infrastructure as Code for configuration management
- **Rolling Updates**: Zero-downtime deployments with health checks

### Rollback Strategy
- Keep last 3 versions of container images in local registry
- Automated rollback on health check failure
- Manual rollback via CI/CD pipeline or kubectl/docker-compose
- Database migrations must be backward compatible
- Automated backup before each deployment

---

**Document Version**: 1.0
**Last Updated**: 2025-10-17
**Maintained By**: Development Team
