# R2.ShopNet Naming Convention - Shopping Platform

## Project Naming Standard

**ALL projects in the R2.ShopNet Shopping Platform MUST start with the `R2.ShopNet` prefix.**

---

## Microservices Project Names

### Core E-Commerce Services

| Service | Project Name | Description |
|---------|-------------|-------------|
| Catalog Service | `R2.ShopNet.Catalog` | Product catalog and categories |
| Shopping Cart Service | `R2.ShopNet.Cart` | Shopping cart management |
| Order Service | `R2.ShopNet.Orders` | Order processing and management |
| Payment Service | `R2.ShopNet.Payment` | Payment processing |
| Search Service | `R2.ShopNet.Search` | Product search and filtering |

### Warehouse & Inventory Services

| Service | Project Name | Description |
|---------|-------------|-------------|
| Inventory Service | `R2.ShopNet.Inventory` | Real-time inventory tracking |
| Warehouse Service | `R2.ShopNet.Warehouse` | Warehouse operations and fulfillment |

### Delivery & Logistics Services

| Service | Project Name | Description |
|---------|-------------|-------------|
| Delivery Service | `R2.ShopNet.Delivery` | Delivery management and tracking |
| Notifications Service | `R2.ShopNet.Notifications` | Email/SMS/Push notifications |

### User & Security Services

| Service | Project Name | Description |
|---------|-------------|-------------|
| Identity Service | `R2.ShopNet.Identity` | Authentication and user management |
| Authorization Service | `R2.ShopNet.Authorization` | Roles and permissions |

### Support Services

| Service | Project Name | Description |
|---------|-------------|-------------|
| Analytics Service | `R2.ShopNet.Analytics` | Business analytics and reporting |
| API Gateway | `R2.ShopNet.ApiGateway` | Request routing and API management |

---

## Web Applications

| Application | Project Name | Description | Technology |
|------------|-------------|-------------|------------|
| Shopping Site | `R2.ShopNet.Web.Shopping` | Customer e-commerce website | Blazor/MVC + React |
| Warehouse App | `R2.ShopNet.Web.Warehouse` | Warehouse management system | Blazor Server |
| Delivery App | `R2.ShopNet.Web.Delivery` or `R2.ShopNet.Mobile.Delivery` | Driver delivery app | .NET MAUI/PWA |
| Admin Portal | `R2.ShopNet.Web.Admin` | Administration dashboard | Blazor Server |

---

## Framework Libraries

| Library Type | Project Name | Description |
|--------------|-------------|-------------|
| Common Framework | `R2.ShopNet.Framework.Common` | Common utilities, Result<T> pattern, DTOs |
| CQRS Framework | `R2.ShopNet.Framework.CQRS` | Custom CQRS implementation |
| Validation Framework | `R2.ShopNet.Framework.Validation` | Custom validation framework |
| Event Framework | `R2.ShopNet.Framework.Events` | Event bus and integration events |
| Service Discovery | `R2.ShopNet.Framework.ServiceDiscovery` | Consul service discovery and registration |
| Configuration | `R2.ShopNet.Framework.Configuration` | Consul configuration provider |

---

## Project Structure

### Full Service Structure

```
R2.ShopNet.{ServiceName}/
├── src/
│   ├── R2.ShopNet.{ServiceName}.API/
│   ├── R2.ShopNet.{ServiceName}.Application/
│   ├── R2.ShopNet.{ServiceName}.Domain/
│   └── R2.ShopNet.{ServiceName}.Infrastructure/
├── tests/
│   ├── R2.ShopNet.{ServiceName}.UnitTests/
│   ├── R2.ShopNet.{ServiceName}.IntegrationTests/
│   └── R2.ShopNet.{ServiceName}.ArchitectureTests/
└── R2.ShopNet.{ServiceName}.sln
```

### Example: Catalog Service

```
R2.ShopNet.Catalog/
├── src/
│   ├── R2.ShopNet.Catalog.API/
│   ├── R2.ShopNet.Catalog.Application/
│   ├── R2.ShopNet.Catalog.Domain/
│   └── R2.ShopNet.Catalog.Infrastructure/
├── tests/
│   ├── R2.ShopNet.Catalog.UnitTests/
│   ├── R2.ShopNet.Catalog.IntegrationTests/
│   └── R2.ShopNet.Catalog.ArchitectureTests/
└── R2.ShopNet.Catalog.sln
```

### Example: Order Service

```
R2.ShopNet.Orders/
├── src/
│   ├── R2.ShopNet.Orders.API/
│   ├── R2.ShopNet.Orders.Application/
│   ├── R2.ShopNet.Orders.Domain/
│   └── R2.ShopNet.Orders.Infrastructure/
├── tests/
│   ├── R2.ShopNet.Orders.UnitTests/
│   ├── R2.ShopNet.Orders.IntegrationTests/
│   └── R2.ShopNet.Orders.ArchitectureTests/
└── R2.ShopNet.Orders.sln
```

---

## Namespace Convention

**Format**: `R2.ShopNet.{ServiceName}.{Layer}.{Feature}`

### Namespace Examples

#### Catalog Service - Commands
```csharp
namespace R2.ShopNet.Catalog.Application.Commands.CreateProduct;

public class CreateProductCommand : ICommand<Result<ProductDto>>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    // Implementation
}

public class CreateProductCommandValidator : IValidator<CreateProductCommand>
{
    // Validation logic
}
```

#### Order Service - Queries
```csharp
namespace R2.ShopNet.Orders.Application.Queries.GetOrder;

public class GetOrderByIdQuery : IQuery<Result<OrderDto>>
{
    public int OrderId { get; set; }
}

public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    // Implementation
}
```

#### Inventory Service - Domain Models
```csharp
namespace R2.ShopNet.Inventory.Domain.Entities;

public class Stock : AggregateRoot
{
    public int ProductId { get; private set; }
    public int WarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity => Quantity - ReservedQuantity;

    public void Reserve(int quantity)
    {
        if (quantity > AvailableQuantity)
            throw new InsufficientStockException();

        ReservedQuantity += quantity;
        AddDomainEvent(new StockReservedDomainEvent(ProductId, quantity));
    }
}

namespace R2.ShopNet.Inventory.Domain.ValueObjects;

public class StockLevel : ValueObject
{
    public int Quantity { get; private set; }
    public int LowStockThreshold { get; private set; }

    public bool IsLowStock => Quantity <= LowStockThreshold;
}

namespace R2.ShopNet.Inventory.Domain.Events;

public class StockReservedDomainEvent : IDomainEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime OccurredOn { get; set; }
}
```

#### API Controllers
```csharp
namespace R2.ShopNet.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var query = new GetProductByIdQuery { ProductId = id };
        var result = await _queryDispatcher.DispatchAsync(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _commandDispatcher.DispatchAsync(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Value.Id }, result.Value);
    }
}
```

#### Infrastructure
```csharp
namespace R2.ShopNet.Catalog.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Product> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}

namespace R2.ShopNet.Catalog.Infrastructure.Configuration;

public class ProductEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
    }
}
```

---

## Class Naming Conventions

### E-Commerce Specific Commands

#### Catalog Service
- `CreateProductCommand`
- `UpdateProductCommand`
- `DeleteProductCommand`
- `AddProductVariantCommand`
- `UpdateProductPriceCommand`
- `SetProductStockCommand`

#### Cart Service
- `AddCartItemCommand`
- `UpdateCartItemQuantityCommand`
- `RemoveCartItemCommand`
- `ClearCartCommand`
- `ApplyCouponCommand`

#### Order Service
- `CreateOrderCommand`
- `CancelOrderCommand`
- `ConfirmPaymentCommand`
- `ShipOrderCommand`
- `CompleteOrderCommand`

#### Inventory Service
- `ReserveStockCommand`
- `ReleaseStockCommand`
- `AdjustStockCommand`
- `TransferStockCommand`

#### Warehouse Service
- `ReceiveInventoryCommand`
- `PickOrderCommand`
- `PackOrderCommand`
- `ShipOrderCommand`

#### Delivery Service
- `AssignDeliveryCommand`
- `UpdateDeliveryStatusCommand`
- `CompleteDeliveryCommand`
- `CancelDeliveryCommand`

### E-Commerce Specific Queries

#### Catalog Service
- `GetProductByIdQuery`
- `GetProductListQuery`
- `SearchProductsQuery`
- `GetProductsByCategoryQuery`
- `GetFeaturedProductsQuery`

#### Cart Service
- `GetCartQuery`
- `GetCartTotalQuery`

#### Order Service
- `GetOrderByIdQuery`
- `GetOrdersByUserIdQuery`
- `GetOrderStatusQuery`

#### Inventory Service
- `GetStockByProductIdQuery`
- `GetLowStockProductsQuery`
- `GetStockByWarehouseQuery`

#### Warehouse Service
- `GetPendingOrdersQuery`
- `GetOrderPickListQuery`

#### Delivery Service
- `GetDeliveryByIdQuery`
- `GetDeliveriesByDriverQuery`
- `GetActiveDeliveriesQuery`

### DTOs (Data Transfer Objects)

#### Catalog
- `ProductDto`
- `ProductListItemDto`
- `ProductDetailDto`
- `CategoryDto`
- `ProductVariantDto`

#### Cart
- `CartDto`
- `CartItemDto`
- `CartSummaryDto`

#### Order
- `OrderDto`
- `OrderItemDto`
- `OrderSummaryDto`
- `InvoiceDto`

#### Inventory
- `StockDto`
- `StockLevelDto`
- `WarehouseDto`

#### Delivery
- `DeliveryDto`
- `DeliveryRouteDto`
- `DriverDto`
- `DeliveryLocationDto`

### Domain Events

#### Catalog Events
- `ProductCreatedDomainEvent`
- `ProductUpdatedDomainEvent`
- `ProductPriceChangedDomainEvent`

#### Order Events
- `OrderCreatedDomainEvent`
- `OrderConfirmedDomainEvent`
- `OrderShippedDomainEvent`
- `OrderDeliveredDomainEvent`
- `OrderCancelledDomainEvent`

#### Inventory Events
- `StockReservedDomainEvent`
- `StockReleasedDomainEvent`
- `StockAdjustedDomainEvent`
- `LowStockDetectedDomainEvent`

#### Delivery Events
- `DeliveryAssignedDomainEvent`
- `DeliveryStartedDomainEvent`
- `DeliveryCompletedDomainEvent`

### Integration Events (Cross-Service)

#### Order to Inventory
- `OrderCreatedIntegrationEvent` → Reserve stock
- `OrderCancelledIntegrationEvent` → Release stock

#### Inventory to Catalog
- `StockUpdatedIntegrationEvent` → Update product availability

#### Order to Delivery
- `OrderShippedIntegrationEvent` → Create delivery

#### Order to Notifications
- `OrderConfirmedIntegrationEvent` → Send confirmation email
- `OrderDeliveredIntegrationEvent` → Send delivery notification

---

## Examples Summary

### ✅ Correct Naming

```
✅ R2.ShopNet.Catalog.API
✅ R2.ShopNet.Orders.Application
✅ R2.ShopNet.Inventory.Infrastructure
✅ R2.ShopNet.Delivery.Domain
✅ namespace R2.ShopNet.Catalog.Application.Commands.CreateProduct;
✅ public class CreateProductCommand : ICommand<Result<ProductDto>>
✅ public class ProductRepository : IProductRepository
✅ R2.ShopNet.Web.Shopping
✅ R2.ShopNet.Mobile.Delivery
```

### ❌ Incorrect Naming

```
❌ Catalog.API (missing R2.ShopNet prefix)
❌ ShopNet.Catalog.API (missing R2 prefix)
❌ CatalogService.Application (wrong service name format)
❌ namespace Catalog.Commands; (missing full namespace)
❌ public class ProductCreateCommand (wrong command naming)
❌ Shopping.Web (wrong prefix order)
```

---

## Web Application Structure

### Shopping Site Example
```
R2.ShopNet.Web.Shopping/
├── Pages/
│   ├── Index.razor
│   ├── Products/
│   │   ├── List.razor
│   │   ├── Detail.razor
│   │   └── Search.razor
│   ├── Cart/
│   │   ├── View.razor
│   │   └── Checkout.razor
│   └── Account/
│       ├── Login.razor
│       ├── Register.razor
│       └── Orders.razor
├── Components/
│   ├── ProductCard.razor
│   ├── CartSummary.razor
│   └── Navigation.razor
├── Services/
│   ├── CatalogService.cs
│   ├── CartService.cs
│   └── OrderService.cs
└── wwwroot/
```

### Warehouse App Example
```
R2.ShopNet.Web.Warehouse/
├── Pages/
│   ├── Dashboard.razor
│   ├── Receiving/
│   │   └── Index.razor
│   ├── Picking/
│   │   ├── Queue.razor
│   │   └── Pick.razor
│   ├── Packing/
│   │   └── Pack.razor
│   └── Inventory/
│       ├── List.razor
│       └── Adjust.razor
└── Services/
```

### Delivery App Example
```
R2.ShopNet.Mobile.Delivery/
├── Views/
│   ├── DashboardPage.xaml
│   ├── DeliveryListPage.xaml
│   ├── DeliveryDetailPage.xaml
│   ├── NavigationPage.xaml
│   └── ProofOfDeliveryPage.xaml
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── DeliveryListViewModel.cs
│   └── DeliveryDetailViewModel.cs
└── Services/
    ├── DeliveryService.cs
    ├── LocationService.cs
    └── NavigationService.cs
```

### Admin Portal Example
```
R2.ShopNet.Web.Admin/
├── Pages/
│   ├── Dashboard.razor
│   ├── Users/
│   │   ├── List.razor
│   │   ├── Create.razor
│   │   └── Edit.razor
│   ├── Roles/
│   │   └── Manage.razor
│   ├── Orders/
│   │   ├── List.razor
│   │   └── Detail.razor
│   └── Settings/
│       └── System.razor
└── Services/
```

---

## Quick Reference Checklist

When creating a new project or class, verify:

- [ ] Does it start with `R2.ShopNet`?
- [ ] Does the namespace follow: `R2.ShopNet.{ServiceName}.{Layer}.{Feature}`?
- [ ] Does the class name follow the appropriate pattern (Command, Query, Handler, etc.)?
- [ ] Is the file name exactly the same as the class name?
- [ ] Is async method name ending with `Async`?
- [ ] Are interfaces prefixed with `I`?
- [ ] Are private fields prefixed with `_`?
- [ ] Are domain events suffixed with `DomainEvent`?
- [ ] Are integration events suffixed with `IntegrationEvent`?

---

## .NET Solution Structure

### Creating a New Service (e.g., Catalog)

```bash
# Create solution directory
mkdir R2.ShopNet.Catalog
cd R2.ShopNet.Catalog

# Create solution
dotnet new sln -n R2.ShopNet.Catalog

# Create directories
mkdir src tests

# API Layer
dotnet new webapi -n R2.ShopNet.Catalog.API -o src/R2.ShopNet.Catalog.API

# Application Layer
dotnet new classlib -n R2.ShopNet.Catalog.Application -o src/R2.ShopNet.Catalog.Application

# Domain Layer
dotnet new classlib -n R2.ShopNet.Catalog.Domain -o src/R2.ShopNet.Catalog.Domain

# Infrastructure Layer
dotnet new classlib -n R2.ShopNet.Catalog.Infrastructure -o src/R2.ShopNet.Catalog.Infrastructure

# Test Projects
dotnet new xunit -n R2.ShopNet.Catalog.UnitTests -o tests/R2.ShopNet.Catalog.UnitTests
dotnet new xunit -n R2.ShopNet.Catalog.IntegrationTests -o tests/R2.ShopNet.Catalog.IntegrationTests

# Add projects to solution
dotnet sln add src/R2.ShopNet.Catalog.API
dotnet sln add src/R2.ShopNet.Catalog.Application
dotnet sln add src/R2.ShopNet.Catalog.Domain
dotnet sln add src/R2.ShopNet.Catalog.Infrastructure
dotnet sln add tests/R2.ShopNet.Catalog.UnitTests
dotnet sln add tests/R2.ShopNet.Catalog.IntegrationTests

# Add project references
dotnet add src/R2.ShopNet.Catalog.API reference src/R2.ShopNet.Catalog.Application
dotnet add src/R2.ShopNet.Catalog.API reference src/R2.ShopNet.Catalog.Infrastructure
dotnet add src/R2.ShopNet.Catalog.Application reference src/R2.ShopNet.Catalog.Domain
dotnet add src/R2.ShopNet.Catalog.Infrastructure reference src/R2.ShopNet.Catalog.Domain
```

---

**Document Version**: 1.0
**Last Updated**: 2025-10-17
**Maintained By**: Development Team
**Status**: Mandatory Standard

**Important**: This naming convention is MANDATORY for all R2.ShopNet projects. Non-compliance will result in code review rejection.
