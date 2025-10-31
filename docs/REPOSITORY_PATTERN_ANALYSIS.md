# Catalog Service Repository Pattern Analysis

## Executive Summary

The Catalog service currently has **one MinIO repository implementation**: `ProductImageRepository`. This analysis identifies common patterns and rules that should be abstracted into a generic base class for future repositories handling similar file/storage operations.

---

## Current Implementation Overview

### 1. Single Repository Implementation

**File:** `/Volumes/Secure/Projects/R2.ShopNet/src/Services/Catalog/R2.ShopNet.Catalog.Infrastructure/Repositories/ProductImageRepository.cs`

**Class:** `ProductImageRepository : IMinIORepository<Product>`

**Size:** 355 lines of code

---

## Common Patterns Identified

### Pattern 1: DbContext Access

**Usage Pattern:**
```csharp
private readonly CatalogDbContext _dbContext;

// Query patterns:
var entity = await _dbContext.ProductImages
    .FirstOrDefaultAsync(pi => pi.Id == fileId, cancellationToken);

var exists = await _dbContext.Products
    .AnyAsync(p => p.Id == entityId, cancellationToken);

var items = await _dbContext.ProductImages
    .Where(pi => pi.ProductId == entityId)
    .OrderBy(pi => pi.DisplayOrder)
    .ThenBy(pi => pi.CreatedAt)
    .ToListAsync(cancellationToken);

// Mutation patterns:
_dbContext.ProductImages.Add(productImage);
await _dbContext.SaveChangesAsync(cancellationToken);
```

**Abstraction Candidates:**
- Generic DbSet access for file entities (TFileEntity)
- Generic DbSet access for parent entities (TEntity)
- SaveChangesAsync wrapper
- Standard query patterns for finding entities by ID

---

### Pattern 2: Storage Service Integration

**Usage Pattern:**
```csharp
private readonly IObjectStorageService _storageService;

// Upload flow:
string objectKey = await _storageService.UploadAsync(
    stream,
    uniqueFileName,
    file.ContentType,
    prefix: $"products/{entityId}",
    cancellationToken);

// Download flow:
var url = await _storageService.GetPresignedUrlAsync(
    productImage.ObjectKey,
    expiryMinutes: 60,
    cancellationToken);

// Delete flow:
var deleted = await _storageService.DeleteAsync(
    productImage.ObjectKey,
    cancellationToken);
```

**Abstraction Candidates:**
- Protected method for uploading with configurable prefix pattern
- Protected method for generating presigned URLs
- Protected method for deleting from storage

---

### Pattern 3: File Validation

**Validation Rules:**
```csharp
// Allowed types as HashSet
private static readonly HashSet<string> AllowedContentTypes = new()
{
    "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
};

// Max file size constant
private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

// Validation logic:
- Check file is not empty
- Check file size doesn't exceed maximum
- Check content type is in allowed list
- Validate file extension matches content type
```

**Abstraction Candidates:**
- Abstract method `ValidateFile()` - overridable in subclasses
- Protected properties for allowed types and max size
- File extension validation helper

---

### Pattern 4: Logging

**Logging Pattern:**
```csharp
private readonly ILogger<ProductImageRepository> _logger;

// Used throughout for:
LogInformation() - successful operations (uploads, deletions, retrieval)
LogWarning() - expected failures (entity not found, file missing)
LogError() - unexpected exceptions
```

**Abstraction Candidates:**
- Protected logger field in base class
- Standard message patterns for common operations

---

### Pattern 5: Entity Existence Validation

**Pattern:**
```csharp
// Validate parent entity exists before operations
var productExists = await _dbContext.Products
    .AnyAsync(p => p.Id == entityId, cancellationToken);

if (!productExists)
{
    throw new InvalidOperationException($"Product with ID {entityId} not found");
}
```

**Abstraction Candidates:**
- Protected abstract method `ValidateEntityExistsAsync(Guid entityId)`
- Exception handling strategy

---

### Pattern 6: Metadata Management

**Metadata Operations:**

**Upload metadata:**
```csharp
var metadata = new Dictionary<string, string>
{
    { "displayOrder", request.DisplayOrder.ToString() },
    { "isPrimary", request.IsPrimary.ToString() }
};
if (!string.IsNullOrWhiteSpace(request.AltText))
{
    metadata["altText"] = request.AltText;
}
```

**Update metadata:**
```csharp
public async Task UpdateFileMetadataAsync(
    Guid fileId,
    Dictionary<string, string> metadata,
    CancellationToken cancellationToken = default)
{
    // Parse metadata dictionary
    // Extract specific fields
    // Call entity.UpdateMetadata()
    // SaveChanges()
}
```

**Abstraction Candidates:**
- Protected method for extracting/parsing metadata from dictionary
- Abstract method `UpdateEntityMetadata()` for entity-specific logic

---

### Pattern 7: File Association Pattern

**Database Storage Pattern:**
```csharp
// File entity creation with all metadata
var productImage = new ProductImage(
    productId: entityId,
    objectKey: objectKey,
    fileName: file.FileName,
    contentType: file.ContentType,
    sizeInBytes: file.Length,
    altText: altText,
    displayOrder: displayOrder,
    isPrimary: isPrimary);

_dbContext.ProductImages.Add(productImage);
await _dbContext.SaveChangesAsync(cancellationToken);
```

**Abstraction Candidates:**
- Protected method for persisting file entity to database
- Abstract method for creating the file entity (TFileEntity creation)

---

### Pattern 8: Batch Operations

**Delete all files for entity:**
```csharp
public async Task DeleteAllFilesAsync(
    Guid entityId,
    CancellationToken cancellationToken = default)
{
    var productImages = await _dbContext.ProductImages
        .Where(pi => pi.ProductId == entityId)
        .ToListAsync(cancellationToken);

    foreach (var image in productImages)
    {
        await _storageService.DeleteAsync(image.ObjectKey, cancellationToken);
    }

    _dbContext.ProductImages.RemoveRange(productImages);
    await _dbContext.SaveChangesAsync(cancellationToken);

    _logger.LogInformation(
        "Deleted {Count} images for product {ProductId}",
        productImages.Count,
        entityId);
}
```

**Abstraction Candidates:**
- Protected method for batch deletion from storage
- Protected method for batch deletion from database
- Abstract method for filtering files by entity ID

---

### Pattern 9: URL Generation and Batch Retrieval

**Pattern:**
```csharp
public async Task<List<FileMetadataDto>> GetFilesWithUrlsAsync(
    Guid entityId,
    int expiryMinutes = 60,
    CancellationToken cancellationToken = default)
{
    // Fetch from DB
    var items = await _dbContext.ProductImages
        .Where(pi => pi.ProductId == entityId)
        .OrderBy(pi => pi.DisplayOrder)
        .ThenBy(pi => pi.CreatedAt)
        .ToListAsync(cancellationToken);

    var result = new List<FileMetadataDto>();

    // For each item, generate URL and build DTO
    foreach (var image in items)
    {
        var url = await _storageService.GetPresignedUrlAsync(
            image.ObjectKey,
            expiryMinutes,
            cancellationToken);

        result.Add(new FileMetadataDto
        {
            Id = image.Id,
            Url = url,
            // ... other properties
            Metadata = new Dictionary<string, string>
            {
                { "altText", image.AltText ?? string.Empty },
                { "isPrimary", image.IsPrimary.ToString() }
            }
        });
    }

    return result;
}
```

**Abstraction Candidates:**
- Protected method for batch URL generation
- Abstract method for building FileMetadataDto from file entity

---

### Pattern 10: IMinIORepository<T> Implementation

**Current Contract:**
```csharp
public interface IMinIORepository<TEntity> where TEntity : class
{
    Task<FileMetadataDto> UploadFileAsync(
        Guid entityId,
        IFormFile file,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(
        Guid fileId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    Task<List<FileMetadataDto>> GetFilesWithUrlsAsync(
        Guid entityId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    Task DeleteAllFilesAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);

    Task UpdateFileMetadataAsync(
        Guid fileId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}
```

**Abstraction Strategy:**
- Keep interface unchanged
- Create abstract base class implementing most of the interface
- Require subclasses to implement entity-specific methods

---

## Abstraction Strategy

### Proposed Base Class Hierarchy

```
DbContext (CatalogDbContext)
    ↓
IMinIORepository<TEntity>
    ↑
    |
GenericMinIORepositoryBase<TEntity, TFileEntity>
    ↑
    |
ProductImageRepository
```

### Generic Base Class: MinIORepositoryBase<TEntity, TFileEntity>

**Type Constraints:**
- `TEntity : BaseEntity` - The parent entity (Product, User, etc.)
- `TFileEntity : FileEntity` - The file storage entity (ProductImage, UserProfilePicture, etc.)

**Protected Abstract Members:**
```csharp
protected abstract string GetStoragePrefix(Guid entityId);
protected abstract Task ValidateEntityExistsAsync(Guid entityId, CancellationToken cancellationToken);
protected abstract DbSet<TFileEntity> GetFileEntitySet();
protected abstract Task<TFileEntity?> FindFileEntityAsync(Guid fileId, CancellationToken cancellationToken);
protected abstract Task<List<TFileEntity>> FindFileEntitiesByEntityIdAsync(Guid entityId, CancellationToken cancellationToken);
protected abstract HashSet<string> GetAllowedContentTypes();
protected abstract long GetMaxFileSizeBytes();
protected abstract TFileEntity CreateFileEntity(
    Guid entityId,
    string objectKey,
    string fileName,
    string contentType,
    long sizeInBytes,
    Dictionary<string, string>? metadata);
protected abstract void UpdateFileEntityMetadata(TFileEntity entity, Dictionary<string, string> metadata);
protected abstract FileMetadataDto MapToFileMetadataDto(TFileEntity entity, string url);
```

**Protected Concrete Methods:**
```csharp
protected virtual void ValidateFile(IFormFile file, HashSet<string> allowedTypes, long maxSize);
protected virtual async Task<string> GenerateObjectKeyAsync(Guid entityId, IFormFile file);
protected virtual IQueryable<TFileEntity> OrderFileEntities(IQueryable<TFileEntity> query);
protected virtual async Task SaveChangesAsync(CancellationToken cancellationToken);
```

**Protected Field:**
```csharp
protected readonly DbContext Context;
protected readonly IObjectStorageService StorageService;
protected readonly ILogger Logger;
```

---

## Code Duplication Analysis

### Current Duplication in ProductImageRepository

1. **File Validation** (~20 lines)
   - Static HashSet declaration
   - ValidateFile() method with extension/content-type mapping
   
2. **DbContext Query Patterns** (~10 lines per operation)
   - Multiple FirstOrDefaultAsync patterns
   - Multiple Where/OrderBy/ToListAsync patterns
   
3. **Storage Service Calls** (~5 lines per operation)
   - Upload pattern (stream open, upload, log)
   - Download URL generation pattern
   - Delete pattern
   
4. **Metadata Extraction** (~15 lines)
   - Dictionary parsing with TryGetValue
   - Type conversion (int.TryParse, bool.TryParse)
   
5. **DTO Construction** (~20 lines)
   - FileMetadataDto creation with metadata dictionary
   - ProductImage entity to DTO mapping

**Total Estimated Duplication:** ~70-80 lines that would be eliminated in a base class

---

## Framework Patterns Already in Place

### Existing Abstractions to Leverage

1. **Repository<TEntity>** - Generic CRUD repository
   - Already implements IRepository<TEntity>
   - Provides DbContext and DbSet access
   - Soft delete support

2. **FileEntity** - Base class for file-based entities
   - ObjectKey, FileName, ContentType, SizeInBytes properties
   - SetFileMetadata() and UpdateFileMetadata() protected methods
   - Inherits from AuditableSoftDeletableEntity

3. **FileMetadataDto** - Standard file metadata transfer object
   - Id, Url, FileName, ContentType, SizeInBytes, UploadedAt, DisplayOrder, Metadata

4. **IObjectStorageService** - Low-level storage abstraction
   - Upload, Download, Delete, GetPresignedUrl, Exists, List, Copy operations

---

## Integration Points

### DI Registration Pattern

**Current (Program.cs):**
```csharp
builder.Services.AddMinioObjectStorage(builder.Configuration);
builder.Services.AddScoped<IMinIORepository<Product>, ProductImageRepository>();
```

**Would Remain Same** - Base class doesn't change this contract

### Handler Injection Pattern

**Current:**
```csharp
public UploadProductImageCommandHandler(
    IMinIORepository<Product> imageRepository,
    ILogger<UploadProductImageCommandHandler> logger)
```

**Would Remain Same** - Interface contract unchanged

---

## CRUD Operations Breakdown

### Create (Upload)
- Validate entity exists
- Validate file
- Upload to MinIO
- Extract metadata from dictionary
- Handle primary image logic (unset others if primary)
- Create database entity
- Save to database
- Return FileMetadataDto

**Complexity:** High - Entity-specific logic for primary image handling

**Abstraction Level:** 30% abstract, 70% concrete

### Read (GetDownloadUrl)
- Find file entity by ID
- Call storage service for URL
- Return URL

**Complexity:** Low

**Abstraction Level:** 90% abstract, 10% concrete

### Read (GetFilesWithUrlsAsync)
- Query file entities by parent ID with sorting
- For each: generate URL
- Map to FileMetadataDto
- Return list

**Complexity:** Medium

**Abstraction Level:** 50% abstract, 50% concrete

### Update (UpdateFileMetadata)
- Find file entity by ID
- Parse metadata dictionary
- Call entity update method
- Save changes

**Complexity:** Medium

**Abstraction Level:** 70% abstract, 30% concrete

### Delete (DeleteFileAsync)
- Find file entity by ID
- Delete from MinIO
- Delete from database
- Handle deletion failures gracefully

**Complexity:** Low

**Abstraction Level:** 80% abstract, 20% concrete

### Delete (DeleteAllFilesAsync)
- Query all files for entity
- Batch delete from MinIO
- Batch delete from database
- Log results

**Complexity:** Medium

**Abstraction Level:** 70% abstract, 30% concrete

---

## Recommended Base Class Rules

### Rule 1: Constructor Injection
All MinIO repositories must accept:
- `DbContext context`
- `IObjectStorageService storageService`
- `ILogger logger`

### Rule 2: File Entity Association
File entities MUST:
- Inherit from FileEntity
- Have a foreign key to parent entity
- Support soft delete via IsDeleted property
- Implement audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)

### Rule 3: File Validation
All repositories MUST:
- Validate file is not empty
- Validate file size against max limit
- Validate content type against allowed types
- Can extend validation in subclass

### Rule 4: Storage Key Generation
All repositories MUST:
- Generate unique filenames using Guid
- Use configurable prefix pattern based on entity type
- Maintain consistency in naming scheme: `{prefix}/{uniqueGuid}.{extension}`

### Rule 5: Metadata Handling
All repositories MUST:
- Accept metadata as Dictionary<string, string> in upload
- Extract metadata using TryGetValue for safe parsing
- Handle missing optional metadata gracefully
- Convert metadata back to Dictionary for storage

### Rule 6: Database Access Pattern
All repositories MUST:
- Use DbContext.SaveChangesAsync() for mutations
- Include proper error handling for duplicates
- Log all operations (info level for success, warning/error for failures)

### Rule 7: Error Handling
All repositories MUST:
- Throw InvalidOperationException for entity not found
- Throw ArgumentException for invalid input
- Log warnings for recoverable errors
- Log errors for failures, but attempt to continue (e.g., MinIO deletion fails but DB record still deleted)

### Rule 8: IMinIORepository Contract
All implementations MUST:
- Implement all 6 interface methods
- Return FileMetadataDto with populated Url for read operations
- Handle CancellationToken properly throughout
- Support configurable URL expiry for presigned URLs

---

## Files Affected by Base Class Creation

**Would Need Changes:**
- `/src/Framework/R2.ShopNet.Framework.Persistence/Storage/Abstractions/` - Add new base class
- `/src/Services/Catalog/R2.ShopNet.Catalog.Infrastructure/Repositories/ProductImageRepository.cs` - Inherit from base, remove duplicated logic

**Would NOT Change:**
- Interface `IMinIORepository<T>`
- Entity classes (Product, ProductImage)
- Configurations (ProductImageConfiguration)
- Handlers/Controllers
- DI registration
- DbContext

---

## Ready for Implementation

This analysis provides a complete roadmap for creating `MinIORepositoryBase<TEntity, TFileEntity>` that would:
1. Eliminate 70-80 lines of duplicated code
2. Make future file repositories (UserProfilePicture, DocumentAttachment, etc.) much simpler
3. Enforce consistent patterns across all MinIO-based repositories
4. Maintain backward compatibility with existing code
5. Keep the IMinIORepository contract unchanged
