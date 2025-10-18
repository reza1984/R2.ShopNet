# Library Alternatives - Free & Open Source

This document outlines the replacements for paid/commercial libraries with free, open-source alternatives.

## Replaced Libraries

### 1. MediatR → Custom CQRS Implementation
**Reason for Change**: MediatR is free but we can implement CQRS pattern in-house for better control and zero external dependencies.

**Implementation Approach**:
```csharp
// Core interfaces
public interface ICommand<TResponse> { }
public interface IQuery<TResponse> { }
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

// Dispatcher with DI
public interface ICommandDispatcher
{
    Task<TResponse> DispatchAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken);
}

public interface IQueryDispatcher
{
    Task<TResponse> DispatchAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken);
}
```

**Benefits**:
- Full control over pipeline behaviors
- No external dependency
- Easier debugging and customization
- Learning opportunity for team

---

### 2. FluentValidation → Custom Validation Framework
**Reason for Change**: While FluentValidation is free (Apache 2.0), we can use built-in DataAnnotations plus custom validators.

**Implementation Approach**:
```csharp
// Use DataAnnotations for simple validation
public class CreateContentCommand : ICommand<Result<ContentDto>>
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }
}

// Custom validator interface for complex validation
public interface IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken);
}

public class CreateContentCommandValidator : IValidator<CreateContentCommand>
{
    public async Task<ValidationResult> ValidateAsync(CreateContentCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        // Custom business rules
        if (await _contentRepository.ExistsAsync(command.Title, cancellationToken))
        {
            errors.Add(new ValidationError("Title", "Content with this title already exists"));
        }

        return new ValidationResult(errors);
    }
}
```

**Benefits**:
- Leverages built-in .NET framework
- Simpler for basic validation
- Can extend for complex scenarios
- No external dependency

---

### 3. AutoMapper/Mapster → Manual DTO Mapping
**Reason for Change**: No external mapping libraries - use programmatic mapping for full control and better performance.

**Implementation Approach**:
```csharp
// Pattern 1: Direct mapping in handlers
var dto = new ProductDto
{
    Id = product.Id,
    Name = product.Name,
    Price = product.Price,
    CategoryName = product.Category?.Name
};

// Pattern 2: EF Core projection (optimal for queries)
var dtos = await _context.Products
    .AsNoTracking()
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        CategoryName = p.Category.Name
    })
    .ToListAsync(cancellationToken);

// Pattern 3: Extension methods for reusability
public static class ProductMappings
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CategoryName = product.Category?.Name
        };
    }
}

// Usage
var dto = product.ToDto();
var dtos = products.Select(p => p.ToDto()).ToList();
```

**Benefits**:
- Zero external dependencies
- No reflection overhead - better performance
- Full type safety and IntelliSense support
- Easier debugging (no magic)
- EF Core projection only fetches needed columns
- Complete control over mapping logic
- Can include business logic in mappings
- No learning curve for team members

**Best Practices**:
- Use EF Core `.Select()` projection for queries (optimal performance)
- Create `ToDto()` extension methods for reusable mappings
- Keep DTOs immutable using `record` or `init` properties
- Handle null navigation properties with `?.` operator

---

### 4. Duende IdentityServer → OpenIddict
**Reason for Change**: Duende IdentityServer requires paid license for production use. OpenIddict is completely free.

**Package**: `OpenIddict` (NuGet)
**License**: Apache 2.0
**Features**: Full OAuth 2.0 / OpenID Connect support

**Configuration Example**:
```csharp
services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
               .SetAuthorizationEndpointUris("/connect/authorize");

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow()
               .AllowClientCredentialsFlow();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough();
    });
```

**Benefits**:
- Completely free (Apache 2.0 license)
- Well-maintained and production-ready
- Full OAuth 2.0 / OpenID Connect compliance
- Active community support
- No licensing costs or restrictions

---

### 5. ImageSharp → SkiaSharp
**Reason for Change**: ImageSharp requires commercial license for production. SkiaSharp is MIT licensed.

**Package**: `SkiaSharp` (NuGet)
**License**: MIT
**Backed By**: Google (used in Chrome, Android)

**Usage Example**:
```csharp
using SkiaSharp;

// Resize image
using var inputStream = File.OpenRead("input.jpg");
using var original = SKBitmap.Decode(inputStream);
using var resized = original.Resize(new SKImageInfo(800, 600), SKFilterQuality.High);
using var image = SKImage.FromBitmap(resized);
using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
using var outputStream = File.OpenWrite("output.jpg");
data.SaveTo(outputStream);

// Apply filters
using var canvas = new SKCanvas(bitmap);
using var paint = new SKPaint
{
    ColorFilter = SKColorFilter.CreateColorMatrix(sepiaMatrix)
};
canvas.DrawBitmap(bitmap, 0, 0, paint);
```

**Benefits**:
- MIT license (completely free)
- High performance (native C++ backend)
- Battle-tested (used by Google)
- Supports all major image formats
- Advanced features (filters, effects, drawing)

---

### 6. FluentAssertions → xUnit Assert + Custom Helpers
**Reason for Change**: While FluentAssertions is free, xUnit's built-in assertions are sufficient.

**Implementation Approach**:
```csharp
// xUnit built-in assertions
Assert.True(result.IsSuccess);
Assert.NotNull(result.Value);
Assert.Equal("Expected", result.Value.Title);
Assert.Contains("search", result.Value.Content);
Assert.Empty(errors);

// Custom assertion helpers for readability
public static class AssertionExtensions
{
    public static void ShouldBeSuccessful<T>(this Result<T> result)
    {
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.Error}");
    }

    public static void ShouldHaveError(this Result result, string expectedError)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedError, result.Error);
    }
}

// Usage
result.ShouldBeSuccessful();
result.ShouldHaveError("Validation failed");
```

**Benefits**:
- Zero external dependencies
- Built into xUnit
- Can create custom helpers as needed
- Simpler to maintain

---

### 7. Moq → NSubstitute
**Reason for Change**: Both are free, but NSubstitute has simpler syntax and MIT license.

**Package**: `NSubstitute` (NuGet)
**License**: MIT

**Usage Example**:
```csharp
// Create mock
var repository = Substitute.For<IContentRepository>();

// Setup behavior
repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(new Content { Id = 1, Title = "Test" }));

// Verify calls
await repository.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
repository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

// Argument matching
repository.SaveAsync(Arg.Is<Content>(c => c.Title == "Test"), Arg.Any<CancellationToken>());
```

**Benefits**:
- MIT license (completely free)
- Simpler, more readable syntax
- Less boilerplate than Moq
- Excellent documentation

---

## Summary of Changes

| Original Library | Replacement | License | Cost |
|-----------------|-------------|---------|------|
| MediatR | Custom CQRS | N/A | Free |
| FluentValidation | DataAnnotations + Custom | N/A | Free |
| AutoMapper / Mapster | Manual DTO Mapping | N/A | Free |
| Duende IdentityServer | OpenIddict | Apache 2.0 | Free |
| ImageSharp | SkiaSharp | MIT | Free |
| FluentAssertions | xUnit + Custom | N/A | Free |
| Moq | NSubstitute | MIT | Free |

## Data Access Strategy

### Entity Framework Core 9 - Complete ORM Solution
**Decision**: Use EF Core 9 exclusively for ALL data access operations (both reads and writes).

**Why EF Core Only (No Dapper)**:
- **CQRS Performance**: EF Core with `AsNoTracking()` for queries provides excellent read performance
- **Compiled Queries**: EF Core compiled queries offer performance comparable to Dapper for hot paths
- **Consistency**: Single ORM reduces complexity and maintenance overhead
- **Advanced Features**: Full support for migrations, change tracking, transactions, and relationships
- **Type Safety**: Complete compile-time checking for all database operations

**Implementation Approach**:
```csharp
// Commands (Write Operations) - Use full EF Core with change tracking
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

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ProductDto>.Success(product.Adapt<ProductDto>());
    }
}

// Queries (Read Operations) - Use AsNoTracking for performance
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
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name
            })
            .ToListAsync(cancellationToken);

        return Result<List<ProductDto>>.Success(products);
    }
}

// High-Performance Queries - Use Compiled Queries for hot paths
public static class ProductQueries
{
    private static readonly Func<ApplicationDbContext, int, Task<Product>> GetByIdCompiled =
        EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
            context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id));

    public static Task<Product> GetByIdAsync(ApplicationDbContext context, int id)
    {
        return GetByIdCompiled(context, id);
    }
}
```

**Performance Optimizations**:
- `AsNoTracking()` for all read-only queries
- `AsSplitQuery()` for complex queries with multiple includes
- Compiled queries for frequently executed queries
- Projection with `Select()` to load only required columns
- Global query filters for soft deletes and multi-tenancy

**Benefits**:
- Single, consistent data access approach
- Excellent performance with proper configuration
- Full type safety and IntelliSense support
- Built-in migration tooling
- No need to learn/maintain multiple ORMs

---

## Additional Free Libraries in Stack

All other libraries are already free and open-source:

- **Serilog** - Apache 2.0 license
- **Polly** - BSD license
- **Entity Framework Core 9** - MIT license (complete data access solution)
- **xUnit** - Apache 2.0 license
- **Testcontainers** - MIT license
- **YARP** - MIT license
- **RabbitMQ.Client** - Apache 2.0 / MPL license
- **StackExchange.Redis** - MIT license
- **Elastic.Clients.Elasticsearch** - Apache 2.0 license
- **Swashbuckle.AspNetCore** - MIT license

---

## Implementation Priority

### Phase 1: Foundation (Immediate)
1. ✅ Custom CQRS implementation
2. ✅ Custom validation framework
3. ✅ Replace with Mapster
4. ✅ Integrate OpenIddict

### Phase 2: Media Processing
5. ✅ Replace with SkiaSharp

### Phase 3: Testing
6. ✅ Switch to NSubstitute
7. ✅ Create custom assertion helpers

---

## Cost Savings

By using only free, open-source libraries with permissive licenses, we achieve:

- **Zero licensing costs** for all libraries
- **No commercial license restrictions**
- **Full control over implementation** (custom CQRS/validation)
- **Production-ready alternatives** (OpenIddict, SkiaSharp, Mapster)
- **Better performance** in some cases (Mapster > AutoMapper)

---

**Document Version**: 1.2
**Last Updated**: 2025-10-18
**Status**: Approved
**Changelog**:
- v1.2 (2025-10-18): Removed Mapster, added comprehensive manual DTO mapping patterns and best practices
- v1.1 (2025-10-18): Removed Dapper, added comprehensive EF Core 9 data access strategy section
- v1.0 (2025-10-17): Initial version with all library replacements
