# Catalog Service Repository Patterns - Quick Reference

## Repository Implementations Found

| File | Class | Entity | Interface | Lines |
|------|-------|--------|-----------|-------|
| `/Repositories/ProductImageRepository.cs` | `ProductImageRepository` | `ProductImage` | `IMinIORepository<Product>` | 355 |

## Common Code Patterns Summary

### Pattern Overview Table

| Pattern | Location | Lines | Frequency | Abstraction Candidate |
|---------|----------|-------|-----------|----------------------|
| DbContext Access | Lines 50-167 | ~30 | 6 methods | Protected methods for query/save |
| Storage Service Calls | Lines 70-76, 150-153, 212-214, 246 | ~15 | 4 operations | Protected abstract methods |
| File Validation | Lines 318-353 | 35 | 1 method | Abstract method (override for different types) |
| Metadata Extraction | Lines 79-84, 276-289 | ~20 | 2 methods | Protected method + abstract method |
| DTO Mapping | Lines 120-134, 178-192 | ~25 | 2 methods | Abstract method |
| Logging | Lines 113-117, 228-231, 252-255, 313-315 | ~15 | 4 locations | Protected method |
| Entity Query Patterns | Lines 50-51, 142-143, 163-167, 202-203, 240-242, 263-264 | ~20 | 6 queries | Base class query helpers |
| Primary Image Logic | Lines 87-97, 293-305 | ~15 | 2 locations | Abstract method |

## File Entity Inheritance Hierarchy

```
BaseEntity (Framework.Common)
    ↓
AuditableSoftDeletableEntity (Framework.Common)
    ↓
FileEntity (Framework.Common)
    ↓
ProductImage (Catalog.Domain)
```

## Repository Dependencies

```
ProductImageRepository
├── CatalogDbContext
│   └── DbSet<ProductImage>
│   └── DbSet<Product>
├── IObjectStorageService (Framework.Persistence)
│   ├── UploadAsync()
│   ├── GetPresignedUrlAsync()
│   ├── DeleteAsync()
│   └── ExistsAsync()
└── ILogger<ProductImageRepository>
```

## CRUD Operations Matrix

| Operation | Method | Validation | Storage | Database | Logs | Complexity |
|-----------|--------|-----------|---------|----------|------|------------|
| Create | UploadFileAsync | Yes | Yes | Yes | Yes | High |
| Read (Single) | GetDownloadUrlAsync | No | Yes | Yes | No | Low |
| Read (Multiple) | GetFilesWithUrlsAsync | No | Yes | Yes | Yes | Medium |
| Update | UpdateFileMetadataAsync | No | No | Yes | Yes | Medium |
| Delete (Single) | DeleteFileAsync | No | Yes | Yes | Yes | Low |
| Delete (Multiple) | DeleteAllFilesAsync | No | Yes | Yes | Yes | Medium |

## Key Rules for MinIO Repositories

| Rule | Current Implementation | Base Class Role |
|------|------------------------|-----------------| 
| **Validation** | Hardcoded types, size, extension | Abstract - subclass provides types/sizes |
| **Storage Prefix** | `products/{entityId}` | Abstract - subclass provides pattern |
| **Entity Validation** | Check Product exists | Abstract - subclass validates specific entity |
| **DbContext Access** | Direct to CatalogDbContext | Protected - base class provides accessors |
| **Metadata Handling** | Dictionary parsing with TryParse | Protected - base class parses, abstract for entity logic |
| **Error Handling** | Throw InvalidOperationException | Documented - base class enforces pattern |
| **Logging** | Info/Warning/Error throughout | Protected - base class provides helpers |
| **File Storage Format** | `{Guid}.{extension}` | Virtual - base class generates, subclass can override |

## Code Metrics

### Current ProductImageRepository Statistics

- **Total Lines:** 355
- **Estimated Duplicable Lines:** 70-80 (20% of code)
- **Public Methods:** 6 (all from IMinIORepository)
- **Private Methods:** 1 (ValidateFile)
- **Static Fields:** 2 (AllowedContentTypes, MaxFileSizeBytes)
- **Instance Fields:** 3 (_dbContext, _storageService, _logger)
- **Constructor Complexity:** Low (simple dependency injection)
- **Method Complexity:** Medium-High (validation, metadata handling, primary image logic)

### Estimated Base Class

- **Protected Abstract Methods:** 9
- **Protected Virtual Methods:** 4
- **Protected Fields:** 3
- **Lines of Code:** 150-200 (handles ~80% of repository logic)

## Integration Points

### Dependency Injection (Program.cs)
```csharp
// Current
builder.Services.AddScoped<IMinIORepository<Product>, ProductImageRepository>();

// Post-refactor (no changes needed)
builder.Services.AddScoped<IMinIORepository<Product>, ProductImageRepository>();
```

### Handler Usage Pattern
```csharp
// All handlers follow this pattern
public class UploadProductImageCommandHandler : ICommandHandler<...>
{
    public UploadProductImageCommandHandler(
        IMinIORepository<Product> imageRepository,
        ILogger<UploadProductImageCommandHandler> logger) { ... }
}
```

### DbContext Pattern
- Repository receives `CatalogDbContext` (can be abstracted to `DbContext`)
- Accesses `DbSet<ProductImage>` and `DbSet<Product>`
- Calls `SaveChangesAsync()` after mutations

## Future Repository Examples

### For UserProfilePicture (Identity Service)
```csharp
public class UserProfilePictureRepository : MinIORepositoryBase<User, UserProfilePicture>
{
    protected override string GetStoragePrefix(Guid entityId) => $"users/{entityId}";
    protected override Task ValidateEntityExistsAsync(Guid userId) => /* check User exists */;
    // ... implement other abstract methods
}
```

### For DocumentAttachment (Orders Service)
```csharp
public class DocumentRepository : MinIORepositoryBase<Order, OrderDocument>
{
    protected override string GetStoragePrefix(Guid entityId) => $"orders/{entityId}/documents";
    protected override HashSet<string> GetAllowedContentTypes() => new() 
    { 
        "application/pdf", "image/png", "image/jpeg" 
    };
    // ... implement other abstract methods
}
```

## File Structure Recommendations

### Current Location
- Repository: `/src/Services/Catalog/R2.ShopNet.Catalog.Infrastructure/Repositories/`
- Interface: `/src/Framework/R2.ShopNet.Framework.Persistence/Storage/Abstractions/`

### Post-Refactor Location
- **Base Class:** `/src/Framework/R2.ShopNet.Framework.Persistence/Storage/Repositories/MinIORepositoryBase.cs`
- **Interface:** (unchanged) `/src/Framework/R2.ShopNet.Framework.Persistence/Storage/Abstractions/IMinIORepository.cs`
- **Concrete:** (simplified) `/src/Services/Catalog/R2.ShopNet.Catalog.Infrastructure/Repositories/ProductImageRepository.cs`

## Quick Stats

- **Services with MinIO repos:** 1 (Catalog)
- **MinIO repositories:** 1 (ProductImageRepository)
- **File entities:** 1 (ProductImage)
- **Code duplication potential:** 70-80 lines
- **Complexity reduction:** ~30% reduction in new MinIO repos
- **Time to implement:** ~4-6 hours (including tests)

## Test Coverage Recommendations

### Base Class Tests
- [ ] File validation (empty, oversized, wrong type)
- [ ] Storage key generation (uniqueness, format)
- [ ] Metadata parsing (missing, invalid types)
- [ ] Error handling (entity not found, storage failure)
- [ ] Query patterns (single, batch, ordering)

### ProductImageRepository Tests
- [ ] Primary image logic (unset others)
- [ ] ProductImage entity creation
- [ ] Metadata mapping (altText, displayOrder, isPrimary)
- [ ] Content type validation (image types only)

## Next Steps

1. Review this analysis with team
2. Create abstract base class `MinIORepositoryBase<TEntity, TFileEntity>`
3. Refactor `ProductImageRepository` to inherit from base
4. Update/add unit tests for base class
5. Document pattern in team wiki
6. Use as template for future file-based repositories
