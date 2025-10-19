# R2.ShopNet.Framework.Persistence

A comprehensive persistence framework implementing the **Repository** and **Unit of Work** patterns with Entity Framework Core support.

## Features

- **Generic Repository Pattern**: Full CRUD operations for all entities
- **Unit of Work Pattern**: Transaction management and change tracking
- **Read-Only Repository**: Optimized for query operations (CQRS read side)
- **Specification Pattern**: Encapsulate complex query logic
- **Soft Delete Support**: Built-in soft delete functionality
- **Pagination Support**: Easy pagination with total count
- **Transaction Management**: Explicit and automatic transaction handling
- **GUID Version 7 Support**: Time-ordered UUIDs for better database performance

## Installation

Add the project reference to your service's Infrastructure layer:

```xml
<ProjectReference Include="..\..\Framework\R2.ShopNet.Framework.Persistence\R2.ShopNet.Framework.Persistence.csproj" />
```

## Quick Start

### 1. Register in Program.cs

```csharp
using R2.ShopNet.Framework.Persistence.Extensions;

// Option 1: Register both UoW and Repositories (Recommended)
builder.Services.AddPersistence<YourDbContext>();

// Option 2: Register individually
builder.Services.AddUnitOfWork<YourDbContext>();
builder.Services.AddRepositories<YourDbContext>();
```

### 2. Basic Usage in Command Handlers

```csharp
using R2.ShopNet.Framework.Persistence.UnitOfWork;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<Product>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Product>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Product>();

        var product = new Product(command.Name, command.Price);
        await repository.AddAsync(product, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Product>.Success(product);
    }
}
```

### 3. Query Operations (CQRS Read Side)

```csharp
using R2.ShopNet.Framework.Persistence.Repositories;

public class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, Result<List<Product>>>
{
    private readonly IReadOnlyRepository<Product> _repository;

    public GetProductsQueryHandler(IReadOnlyRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<Product>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return Result<List<Product>>.Success(products.ToList());
    }
}
```

## Repository Operations

### Query Operations

```csharp
// Get by ID
var product = await repository.GetByIdAsync(productId);

// Get all (excludes soft-deleted by default)
var allProducts = await repository.GetAllAsync();

// Find with predicate
var activeProducts = await repository.FindAsync(p => p.IsActive);

// First or default
var product = await repository.FirstOrDefaultAsync(p => p.Sku == sku);

// Check existence
var exists = await repository.ExistsAsync(productId);
var hasActiveProducts = await repository.ExistsAsync(p => p.IsActive);

// Count
var totalCount = await repository.CountAsync();
var activeCount = await repository.CountAsync(p => p.IsActive);
```

### Pagination

```csharp
// Simple pagination
var (items, totalCount) = await repository.GetPagedAsync(
    pageNumber: 1,
    pageSize: 20);

// Pagination with filter
var (items, totalCount) = await repository.GetPagedAsync(
    p => p.Category == "Electronics",
    pageNumber: 1,
    pageSize: 20);

// Pagination with specification
var spec = new ActiveProductsSpecification();
var (items, totalCount) = await repository.GetPagedAsync(spec, 1, 20);
```

### Command Operations

```csharp
// Add
var product = new Product("Laptop", 999.99m);
await repository.AddAsync(product);
await _unitOfWork.SaveChangesAsync();

// Add multiple
await repository.AddRangeAsync(products);
await _unitOfWork.SaveChangesAsync();

// Update
product.UpdatePrice(899.99m);
await repository.UpdateAsync(product);
await _unitOfWork.SaveChangesAsync();

// Delete (hard delete)
await repository.DeleteAsync(product);
await _unitOfWork.SaveChangesAsync();

// Soft delete (recommended)
await repository.SoftDeleteAsync(productId);
await _unitOfWork.SaveChangesAsync();
```

## Specification Pattern

Encapsulate complex query logic in reusable specifications:

### Create a Specification

```csharp
using R2.ShopNet.Framework.Persistence.Specifications;

public class ActiveProductsWithCategorySpecification : BaseSpecification<Product>
{
    public ActiveProductsWithCategorySpecification(string category)
        : base(p => p.IsActive && p.Category == category)
    {
        // Include related entities
        AddInclude(p => p.Reviews);
        AddInclude(p => p.Supplier);

        // Or use string-based includes for nested includes
        AddInclude("Reviews.User");

        // Apply ordering
        ApplyOrderByDescending(p => p.CreatedAt);

        // Apply pagination
        ApplyPaging(skip: 0, take: 10);

        // Use AsNoTracking for better read performance
        ApplyNoTracking();
    }
}
```

### Use a Specification

```csharp
var spec = new ActiveProductsWithCategorySpecification("Electronics");

// Get results
var products = await repository.FindAsync(spec);

// Get paginated results
var (items, total) = await repository.GetPagedAsync(spec, pageNumber: 1, pageSize: 20);

// Count
var count = await repository.CountAsync(spec);
```

## Transaction Management

### Explicit Transactions

```csharp
try
{
    await _unitOfWork.BeginTransactionAsync();

    var productRepo = _unitOfWork.Repository<Product>();
    var orderRepo = _unitOfWork.Repository<Order>();

    var product = await productRepo.GetByIdAsync(productId);
    product.DecreaseStock(quantity);
    await productRepo.UpdateAsync(product);

    var order = new Order(customerId, productId, quantity);
    await orderRepo.AddAsync(order);

    await _unitOfWork.CommitTransactionAsync();
}
catch (Exception)
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

### Automatic Transaction Scope

```csharp
var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    var productRepo = _unitOfWork.Repository<Product>();
    var orderRepo = _unitOfWork.Repository<Order>();

    var product = await productRepo.GetByIdAsync(productId);
    product.DecreaseStock(quantity);
    await productRepo.UpdateAsync(product);

    var order = new Order(customerId, productId, quantity);
    await orderRepo.AddAsync(order);

    return order;
});
// Transaction is automatically committed or rolled back
```

## Custom Repositories

For entity-specific operations, create custom repositories:

```csharp
public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category);
    Task<Product?> GetBySkuAsync(string sku);
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(DbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(string category)
    {
        return await DbSet
            .Where(p => !p.IsDeleted && p.Category == category)
            .Include(p => p.Reviews)
            .ToListAsync();
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await FirstOrDefaultAsync(p => p.Sku == sku);
    }
}
```

Register custom repository:

```csharp
builder.Services.AddRepository<Product, ProductRepository>();
```

## Best Practices

1. **Use IReadOnlyRepository for Queries**: In CQRS query handlers, use `IReadOnlyRepository<T>` for better performance (AsNoTracking).

2. **Always SaveChanges**: Repository methods don't automatically save changes. Always call `await _unitOfWork.SaveChangesAsync()`.

3. **Prefer Soft Deletes**: Use `SoftDeleteAsync()` instead of `DeleteAsync()` to maintain data history.

4. **Use Specifications for Complex Queries**: Encapsulate complex query logic in specifications for reusability and testability.

5. **Transaction Scope**: Use `ExecuteInTransactionAsync()` for automatic transaction management.

6. **Custom Repositories When Needed**: Create custom repositories only when you need entity-specific operations.

7. **Inject Specific Dependencies**: Inject `IRepository<T>` or `IReadOnlyRepository<T>` directly when you only need single entity operations.

## Integration with CQRS

### Command Handler Example

```csharp
public class UpdateProductPriceCommandHandler
    : ICommandHandler<UpdateProductPriceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UpdateProductPriceCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(
        UpdateProductPriceCommand command,
        CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Repository<Product>();

        var product = await repository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result.Failure(Error.NotFound("Product.NotFound", "Product not found"));

        var oldPrice = product.Price;
        product.UpdatePrice(command.NewPrice);

        await repository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.PublishAsync(
            new ProductPriceChangedEvent(product.Id, oldPrice, command.NewPrice),
            cancellationToken);

        return Result.Success();
    }
}
```

### Query Handler Example

```csharp
public class GetProductDetailsQueryHandler
    : IQueryHandler<GetProductDetailsQuery, Result<ProductDetailsDto>>
{
    private readonly IReadOnlyRepository<Product> _repository;

    public GetProductDetailsQueryHandler(IReadOnlyRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductDetailsDto>> Handle(
        GetProductDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ProductWithDetailsSpecification(query.ProductId);

        var product = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
        if (product == null)
            return Result<ProductDetailsDto>.Failure(
                Error.NotFound("Product.NotFound", "Product not found"));

        var dto = new ProductDetailsDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category
        };

        return Result<ProductDetailsDto>.Success(dto);
    }
}
```

## GUID Version 7 (Time-Ordered UUIDs)

All entities that inherit from `BaseEntity` automatically use **GUID Version 7** for their IDs. This provides significant performance benefits:

### Why GUIDv7?

1. **Better Database Performance**: Time-ordered GUIDs reduce index fragmentation and improve INSERT performance
2. **Natural Sorting**: GUIDs are sortable by creation time
3. **Better Locality**: Sequential IDs improve cache performance and reduce page splits
4. **Still Globally Unique**: Maintains uniqueness across distributed systems

### Example

```csharp
// Creating entities automatically uses GUIDv7
var product = new Product("Laptop", 999.99m);
Console.WriteLine(product.Id); // e.g., 018d1234-5678-7abc-def0-123456789abc

// Generate GUIDv7 manually
var id = GuidGenerator.NewGuidV7();

// Extract timestamp from GUIDv7
var timestamp = GuidGenerator.GetTimestamp(id);

// Validate if GUID is version 7
bool isV7 = GuidGenerator.IsGuidV7(id);
```

### Performance Comparison

**Traditional GUIDv4 (Random)**:
- ❌ Random distribution causes index fragmentation
- ❌ Poor cache locality
- ❌ Increased page splits in B-tree indexes
- ❌ Not sortable by time

**GUIDv7 (Time-Ordered)**:
- ✅ Sequential within time window
- ✅ Better index locality
- ✅ Reduced page splits
- ✅ Sortable by creation time
- ✅ ~30-50% better INSERT performance in tests

### Database Index Configuration

For optimal performance, ensure your database indexes are configured correctly:

```sql
-- PostgreSQL: Use B-tree indexes (default, works great with GUIDv7)
CREATE INDEX idx_products_id ON products (id);

-- SQL Server: Clustered indexes work well with GUIDv7
CREATE CLUSTERED INDEX idx_products_id ON products (id);
```

## Architecture

```
R2.ShopNet.Framework.Persistence/
├── Repositories/
│   ├── IRepository.cs                 # Generic repository interface
│   ├── IReadOnlyRepository.cs         # Read-only repository interface
│   ├── Repository.cs                  # Generic repository implementation
│   └── ReadOnlyRepository.cs          # Read-only repository implementation
├── UnitOfWork/
│   ├── IUnitOfWork.cs                 # Unit of Work interface
│   └── UnitOfWork.cs                  # Unit of Work implementation
├── Specifications/
│   ├── ISpecification.cs              # Specification interface
│   ├── BaseSpecification.cs           # Base specification implementation
│   └── SpecificationEvaluator.cs      # Query specification evaluator
└── Extensions/
    └── ServiceCollectionExtensions.cs # DI registration extensions
```
