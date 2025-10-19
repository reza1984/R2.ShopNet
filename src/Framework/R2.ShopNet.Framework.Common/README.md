# R2.ShopNet.Framework.Common

Core framework components including base entities, result pattern, and GUID Version 7 support.

## Features

- **BaseEntity**: Abstract base class for all domain entities with GUID Version 7 IDs
- **Result Pattern**: Strongly-typed result objects for error handling
- **Error Types**: Predefined error types (NotFound, Validation, Conflict, etc.)
- **GuidGenerator**: GUID Version 7 generation using native .NET implementation
- **IAuditableEntity**: Interface for audit tracking

## GUID Version 7

This framework uses **GUID Version 7** (RFC 9562) for all entity IDs, leveraging .NET's native `Guid.CreateVersion7()` implementation.

### Why GUIDv7?

GUIDv7 provides significant advantages over traditional GUIDv4:

| Feature | GUIDv4 (Random) | GUIDv7 (Time-Ordered) |
|---------|----------------|----------------------|
| **Index Performance** | ❌ Poor - Random distribution causes fragmentation | ✅ Excellent - Sequential insertion |
| **Cache Locality** | ❌ Poor | ✅ Good - Better spatial locality |
| **Page Splits** | ❌ High - Random insertion | ✅ Low - Sequential insertion |
| **Sortable by Time** | ❌ No | ✅ Yes - Naturally ordered |
| **INSERT Performance** | Baseline | ✅ 30-50% faster in tests |
| **Uniqueness** | ✅ Globally unique | ✅ Globally unique |

### Usage

#### Automatic with BaseEntity

All entities inheriting from `BaseEntity` automatically get GUIDv7 IDs:

```csharp
public class Product : BaseEntity
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public Product(string name, decimal price)
    {
        Name = name;
        Price = price;
        // Id is automatically assigned a GUIDv7
    }
}

// Create entity
var product = new Product("Laptop", 999.99m);
Console.WriteLine(product.Id); // e.g., 018d1234-5678-7abc-def0-123456789abc
```

#### Manual Generation

```csharp
using R2.ShopNet.Framework.Common;

// Generate new GUIDv7 with current timestamp
var id = GuidGenerator.NewGuidV7();

// Generate GUIDv7 with specific timestamp
var timestamp = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
var id = GuidGenerator.NewGuidV7(timestamp);

// Extract timestamp from GUIDv7
var extractedTime = GuidGenerator.GetTimestamp(id);
Console.WriteLine($"Created at: {extractedTime}");

// Validate if GUID is version 7
bool isV7 = GuidGenerator.IsGuidV7(id);

// Get version number
int version = GuidGenerator.GetVersion(id);
Console.WriteLine($"Version: {version}"); // 7
```

### GUIDv7 Structure (RFC 9562)

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        unix_ts_ms (48 bits)                   |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|        unix_ts_ms             |  ver  |       rand_a (12)     |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|var|                    rand_b (62 bits)                       |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                         rand_b (cont)                         |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

- **unix_ts_ms**: 48-bit big-endian unsigned number of Unix epoch timestamp in milliseconds
- **ver**: 4-bit version number (0111 = 7)
- **rand_a**: 12 bits of random data
- **var**: 2-bit variant (10 = RFC 9562)
- **rand_b**: 62 bits of random data

### Time Ordering Example

```csharp
var ids = new List<Guid>();

// Generate multiple GUIDs over time
for (int i = 0; i < 5; i++)
{
    ids.Add(GuidGenerator.NewGuidV7());
    Thread.Sleep(100);
}

// GUIDs are naturally sorted by creation time
var sorted = ids.OrderBy(g => g).ToList();
bool isOrdered = ids.SequenceEqual(sorted);
Console.WriteLine($"Naturally ordered: {isOrdered}"); // True

// Print with timestamps
foreach (var id in ids)
{
    var timestamp = GuidGenerator.GetTimestamp(id);
    Console.WriteLine($"{id} - {timestamp:HH:mm:ss.fff}");
}
```

### Database Performance Benefits

#### PostgreSQL Example

```sql
-- Create table with GUIDv7 primary key
CREATE TABLE products (
    id UUID PRIMARY KEY,  -- GUIDv7 provides better B-tree performance
    name VARCHAR(255),
    price DECIMAL(10, 2),
    created_at TIMESTAMPTZ
);

-- Index performance is excellent with GUIDv7
CREATE INDEX idx_products_created ON products (created_at);
```

#### Benefits:

1. **Sequential Inserts**: GUIDs are inserted in order, reducing B-tree rebalancing
2. **Better Cache Hit Rate**: Sequential IDs improve buffer cache efficiency
3. **Reduced Write Amplification**: Fewer page splits mean less I/O
4. **Improved Vacuum Performance**: Sequential data reduces vacuum overhead

### Migration from GUIDv4

If you're migrating from GUIDv4 to GUIDv7:

```csharp
// Old code
var id = Guid.NewGuid(); // GUIDv4

// New code - automatically used in BaseEntity
var entity = new MyEntity(); // Uses GUIDv7

// Manual generation
var id = GuidGenerator.NewGuidV7(); // GUIDv7
```

**Note**: GUIDv7 is backward compatible - you can mix v4 and v7 GUIDs in the same database. New entities will use v7, existing v4 GUIDs continue to work.

## BaseEntity

All domain entities should inherit from `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }          // Auto-assigned GUIDv7
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }    // Soft delete support
}
```

### Features:

- **Auto-Generated GUIDv7**: ID is automatically assigned in constructor
- **Audit Timestamps**: CreatedAt and UpdatedAt tracking
- **Soft Delete**: IsDeleted flag for logical deletion
- **Value Equality**: Proper Equals/GetHashCode based on ID

### Example:

```csharp
public class Order : BaseEntity
{
    public string OrderNumber { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Order(string orderNumber, decimal totalAmount)
    {
        OrderNumber = orderNumber;
        TotalAmount = totalAmount;
        // Id, CreatedAt automatically set
    }

    public void UpdateTotal(decimal newTotal)
    {
        TotalAmount = newTotal;
        Update(); // Sets UpdatedAt
    }
}
```

## Result Pattern

The Result pattern provides type-safe error handling:

```csharp
// Success
var result = Result<Product>.Success(product);

// Failure with error
var result = Result<Product>.Failure(
    Error.NotFound("Product.NotFound", "Product not found")
);

// Check result
if (result.IsSuccess)
{
    var product = result.Value;
}
else
{
    var error = result.Error;
    Console.WriteLine($"{error.Code}: {error.Message}");
}
```

### Error Types:

```csharp
Error.NotFound("code", "message")      // 404
Error.Validation("code", "message")    // 400
Error.Conflict("code", "message")      // 409
Error.Unauthorized("code", "message")  // 401
Error.Forbidden("code", "message")     // 403
Error.Failure("code", "message")       // 500
```

## IAuditableEntity

For entities requiring user audit tracking:

```csharp
public class Document : BaseEntity, IAuditableEntity
{
    public string CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Other properties...
}
```

## .NET Version

This framework requires **.NET 9.0** or later for native GUIDv7 support via `Guid.CreateVersion7()`.

## References

- [RFC 9562 - UUIDs](https://www.rfc-editor.org/rfc/rfc9562.html)
- [GUIDv7 Draft Spec](https://datatracker.ietf.org/doc/html/draft-peabody-dispatch-new-uuid-format)
- [.NET Guid.CreateVersion7 Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.guid.createversion7)
