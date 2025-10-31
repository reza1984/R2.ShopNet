# Repository Pattern Code Locations - Line-by-Line Reference

## File: ProductImageRepository.cs
**Path:** `/Volumes/Secure/Projects/R2.ShopNet/src/Services/Catalog/R2.ShopNet.Catalog.Infrastructure/Repositories/ProductImageRepository.cs`
**Total Lines:** 355

### Pattern 1: Class Declaration and Dependency Injection (Lines 14-41)
```csharp
public class ProductImageRepository : IMinIORepository<Product>
{
    private readonly CatalogDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<ProductImageRepository> _logger;

    public ProductImageRepository(
        CatalogDbContext dbContext,
        IObjectStorageService storageService,
        ILogger<ProductImageRepository> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _logger = logger;
    }
}
```

**Abstraction Point:** 
- Base class should accept generic `DbContext` instead of `CatalogDbContext`
- Base class should manage these three fields
- Constructor can be identical or call base

---

### Pattern 2: Entity Existence Validation (Lines 49-56)
```csharp
// Validate product exists
var productExists = await _dbContext.Products
    .AnyAsync(p => p.Id == entityId, cancellationToken);

if (!productExists)
{
    throw new InvalidOperationException($"Product with ID {entityId} not found");
}
```

**Abstraction Point:**
- Abstract method: `protected abstract Task ValidateEntityExistsAsync(Guid entityId, CancellationToken cancellationToken)`
- Base class enforces this is called before file operations
- Subclass implements entity-specific validation

---

### Pattern 3: File Validation - Definition (Lines 20-28)
```csharp
// Allowed image types
private static readonly HashSet<string> AllowedContentTypes = new()
{
    "image/jpeg",
    "image/jpg",
    "image/png",
    "image/webp",
    "image/gif"
};

// Max file size: 10MB
private const long MaxFileSizeBytes = 10 * 1024 * 1024;
```

**Abstraction Point:**
- Abstract methods: `GetAllowedContentTypes()` and `GetMaxFileSizeBytes()`
- Base class uses these in ValidateFile()
- Allows flexibility for different file types per repository

---

### Pattern 4: File Validation - Implementation (Lines 318-353)
```csharp
private void ValidateFile(IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        throw new ArgumentException("File is empty or null");
    }

    if (file.Length > MaxFileSizeBytes)
    {
        throw new ArgumentException(
            $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB");
    }

    if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
    {
        throw new ArgumentException(
            $"File type {file.ContentType} is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}");
    }

    // Validate file extension matches content type
    var extension = Path.GetExtension(file.FileName).ToLower();
    var expectedExtensions = new Dictionary<string, string[]>
    {
        { "image/jpeg", new[] { ".jpg", ".jpeg" } },
        { "image/png", new[] { ".png" } },
        { "image/webp", new[] { ".webp" } },
        { "image/gif", new[] { ".gif" } }
    };

    if (expectedExtensions.TryGetValue(file.ContentType.ToLower(), out var validExtensions) &&
        !validExtensions.Contains(extension))
    {
        throw new ArgumentException(
            $"File extension {extension} does not match content type {file.ContentType}");
    }
}
```

**Abstraction Point:**
- Protected virtual method in base class with generic validation
- Can be overridden in subclass for additional validation
- Extension mapping could be abstract method: `GetExpectedExtensions()`

---

### Pattern 5: Upload - File Stream Handling (Lines 62-76)
```csharp
// Generate unique filename
var fileExtension = Path.GetExtension(file.FileName);
var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
var prefix = $"products/{entityId}";

// Upload to MinIO
string objectKey;
using (var stream = file.OpenReadStream())
{
    objectKey = await _storageService.UploadAsync(
        stream,
        uniqueFileName,
        file.ContentType,
        prefix,
        cancellationToken);
}
```

**Abstraction Point:**
- Protected method: `GenerateObjectKeyAsync(Guid entityId, IFormFile file)` - generates unique filename
- Abstract method: `GetStoragePrefix(Guid entityId)` - returns prefix pattern
- Base class handles stream and upload orchestration

---

### Pattern 6: Metadata Extraction - Upload (Lines 78-97)
```csharp
// Extract metadata
var altText = metadata?.GetValueOrDefault("altText");
var displayOrderStr = metadata?.GetValueOrDefault("displayOrder", "0");
var isPrimaryStr = metadata?.GetValueOrDefault("isPrimary", "false");

int displayOrder = int.TryParse(displayOrderStr, out var order) ? order : 0;
bool isPrimary = bool.TryParse(isPrimaryStr, out var primary) && primary;

// If this is primary, unset other primary images for this product
if (isPrimary)
{
    var existingPrimaryImages = await _dbContext.ProductImages
        .Where(pi => pi.ProductId == entityId && pi.IsPrimary)
        .ToListAsync(cancellationToken);

    foreach (var img in existingPrimaryImages)
    {
        img.UnmarkAsPrimary();
    }
}
```

**Abstraction Point:**
- Protected method: `ParseMetadata(Dictionary<string, string>? metadata)` - safe extraction
- Abstract method: `ApplyMetadataLogic()` - ProductImage-specific logic (primary image handling)

---

### Pattern 7: Entity Creation (Lines 99-111)
```csharp
// Create database record
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

**Abstraction Point:**
- Abstract method: `CreateFileEntity()` - returns TFileEntity
- Protected method: `PersistToDatabase()` - handles Add and SaveChanges
- Base class orchestrates the overall flow

---

### Pattern 8: Logging - Success (Lines 113-117)
```csharp
_logger.LogInformation(
    "Uploaded product image {ImageId} for product {ProductId} to {ObjectKey}",
    productImage.Id,
    entityId,
    objectKey);
```

**Abstraction Point:**
- Protected method: `LogUploadSuccess()` with parameters
- Base class provides consistent logging patterns
- Subclass can customize message details

---

### Pattern 9: DTO Mapping - Upload (Lines 119-134)
```csharp
// Return metadata DTO
return new FileMetadataDto
{
    Id = productImage.Id,
    Url = string.Empty, // Will be populated when retrieved
    FileName = productImage.FileName,
    ContentType = productImage.ContentType,
    SizeInBytes = productImage.SizeInBytes,
    UploadedAt = productImage.CreatedAt,
    DisplayOrder = productImage.DisplayOrder,
    Metadata = new Dictionary<string, string>
    {
        { "altText", productImage.AltText ?? string.Empty },
        { "isPrimary", productImage.IsPrimary.ToString() }
    }
};
```

**Abstraction Point:**
- Abstract method: `MapToFileMetadataDto(TFileEntity entity, string? url = null)`
- Base class provides standard mapping scaffold
- Subclass populates entity-specific metadata

---

### Pattern 10: Get Download URL - Query (Lines 142-148)
```csharp
var productImage = await _dbContext.ProductImages
    .FirstOrDefaultAsync(pi => pi.Id == fileId, cancellationToken);

if (productImage == null)
{
    throw new InvalidOperationException($"Product image with ID {fileId} not found");
}
```

**Abstraction Point:**
- Abstract method: `FindFileEntityAsync(Guid fileId)` returns TFileEntity?
- Base class provides error handling
- Works with DbSet<TFileEntity> accessed via abstract method

---

### Pattern 11: Get Presigned URL (Lines 150-155)
```csharp
var url = await _storageService.GetPresignedUrlAsync(
    productImage.ObjectKey,
    expiryMinutes,
    cancellationToken);

return url;
```

**Abstraction Point:**
- Protected method in base class using _storageService
- Simple pattern, mostly abstracted already

---

### Pattern 12: Batch Retrieval - Query (Lines 163-167)
```csharp
var productImages = await _dbContext.ProductImages
    .Where(pi => pi.ProductId == entityId)
    .OrderBy(pi => pi.DisplayOrder)
    .ThenBy(pi => pi.CreatedAt)
    .ToListAsync(cancellationToken);
```

**Abstraction Point:**
- Abstract method: `FindFileEntitiesByEntityIdAsync(Guid entityId)`
- Protected virtual method: `OrderFileEntities()` - customizable sorting
- Base class provides filtering

---

### Pattern 13: Batch Retrieval - URL Generation (Lines 171-193)
```csharp
var result = new List<FileMetadataDto>();

foreach (var image in productImages)
{
    var url = await _storageService.GetPresignedUrlAsync(
        image.ObjectKey,
        expiryMinutes,
        cancellationToken);

    result.Add(new FileMetadataDto
    {
        Id = image.Id,
        Url = url,
        FileName = image.FileName,
        ContentType = image.ContentType,
        SizeInBytes = image.SizeInBytes,
        UploadedAt = image.CreatedAt,
        DisplayOrder = image.DisplayOrder,
        Metadata = new Dictionary<string, string>
        {
            { "altText", image.AltText ?? string.Empty },
            { "isPrimary", image.IsPrimary.ToString() }
        }
    });
}

return result;
```

**Abstraction Point:**
- Protected method: `GenerateFilesWithUrls()` - base class handles loop and URL generation
- Abstract method: `MapToFileMetadataDto()` - subclass provides DTO details
- Base class orchestrates overall pattern

---

### Pattern 14: Single Delete - Query (Lines 202-203)
```csharp
var productImage = await _dbContext.ProductImages
    .FirstOrDefaultAsync(pi => pi.Id == fileId, cancellationToken);
```

**Abstraction Point:**
- Abstract method: `FindFileEntityAsync(Guid fileId)` (reused from GetDownloadUrl)

---

### Pattern 15: Single Delete - Storage (Lines 212-214)
```csharp
// Delete from MinIO
var deleted = await _storageService.DeleteAsync(
    productImage.ObjectKey,
    cancellationToken);
```

**Abstraction Point:**
- Protected method - directly use _storageService
- Already well abstracted

---

### Pattern 16: Single Delete - Database (Lines 225-226)
```csharp
_dbContext.ProductImages.Remove(productImage);
await _dbContext.SaveChangesAsync(cancellationToken);
```

**Abstraction Point:**
- Protected method: `RemoveFromDatabase()` - handles DbSet.Remove and SaveChanges
- Works with DbSet<TFileEntity> from abstract method

---

### Pattern 17: Batch Delete - Query (Lines 240-242)
```csharp
var productImages = await _dbContext.ProductImages
    .Where(pi => pi.ProductId == entityId)
    .ToListAsync(cancellationToken);
```

**Abstraction Point:**
- Abstract method: `FindFileEntitiesByEntityIdAsync()` (reused)

---

### Pattern 18: Batch Delete - Operations (Lines 244-250)
```csharp
foreach (var image in productImages)
{
    await _storageService.DeleteAsync(image.ObjectKey, cancellationToken);
}

_dbContext.ProductImages.RemoveRange(productImages);
await _dbContext.SaveChangesAsync(cancellationToken);
```

**Abstraction Point:**
- Protected method: `BatchDeleteFromStorage(List<TFileEntity> entities)`
- Protected method: `RemoveRangeFromDatabase(List<TFileEntity> entities)`

---

### Pattern 19: Update Metadata - Query (Lines 263-264)
```csharp
var productImage = await _dbContext.ProductImages
    .FirstOrDefaultAsync(pi => pi.Id == fileId, cancellationToken);
```

**Abstraction Point:**
- Abstract method: `FindFileEntityAsync()` (reused)

---

### Pattern 20: Update Metadata - Parsing (Lines 271-306)
```csharp
// Prepare new metadata values
string? altText = null;
int? displayOrder = null;
bool? isPrimary = null;

if (metadata.TryGetValue("altText", out var altTextValue))
{
    altText = altTextValue;
}

if (metadata.TryGetValue("displayOrder", out var displayOrderStr) &&
    int.TryParse(displayOrderStr, out var order))
{
    displayOrder = order;
}

if (metadata.TryGetValue("isPrimary", out var isPrimaryStr) &&
    bool.TryParse(isPrimaryStr, out var primary))
{
    isPrimary = primary;

    // If setting as primary, unset other primary images for this product
    if (primary && !productImage.IsPrimary)
    {
        var otherPrimaryImages = await _dbContext.ProductImages
            .Where(pi => pi.ProductId == productImage.ProductId &&
                        pi.Id != fileId &&
                        pi.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var img in otherPrimaryImages)
        {
            img.UnmarkAsPrimary();
        }
    }
}
```

**Abstraction Point:**
- Protected method: `ParseMetadataForUpdate()` - generic parsing
- Abstract method: `ApplyUpdateMetadataLogic()` - ProductImage-specific logic

---

### Pattern 21: Update Metadata - Persistence (Lines 309-311)
```csharp
// Update the product image metadata
productImage.UpdateMetadata(altText, displayOrder, isPrimary);

await _dbContext.SaveChangesAsync(cancellationToken);
```

**Abstraction Point:**
- Abstract method: `UpdateFileEntityMetadata(TFileEntity entity, Dictionary<string, string> metadata)`
- Base class handles SaveChanges

---

## Summary of Abstractable Sections

| Pattern | Lines | Type | Abstraction |
|---------|-------|------|-------------|
| Constructor & DI | 14-41 | Concrete | Base class can standardize |
| Entity Validation | 49-56 | Validation | Abstract method |
| Allowed Types (const) | 20-28 | Configuration | Abstract method (GetAllowedContentTypes) |
| File Validation | 318-353 | Validation | Protected virtual method (customizable) |
| File Generation | 62-76 | Utility | Protected method (GenerateObjectKey) + Abstract (GetStoragePrefix) |
| Metadata Extraction | 78-97 | Utility | Protected method (ParseMetadata) + Abstract (ApplyMetadataLogic) |
| Entity Creation | 99-111 | Factory | Abstract method (CreateFileEntity) |
| Logging | 113-117+ | Utility | Protected method (LogUploadSuccess, etc.) |
| DTO Mapping | 119-134, 178-192 | Mapper | Abstract method (MapToFileMetadataDto) |
| Query Operations | Multiple | Query | Abstract methods (FindFileEntityAsync, FindFileEntitiesByEntityIdAsync) |
| Storage Operations | Multiple | I/O | Protected methods using _storageService |
| Database Operations | Multiple | I/O | Protected methods (PersistToDatabase, RemoveFromDatabase, etc.) |

