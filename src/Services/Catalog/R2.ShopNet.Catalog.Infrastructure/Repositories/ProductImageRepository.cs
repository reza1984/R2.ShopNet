using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Persistence.Storage;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.DTOs;

namespace R2.ShopNet.Catalog.Infrastructure.Repositories;

/// <summary>
/// Repository for managing product images in MinIO storage.
/// Inherits from MinIORepositoryBase to enforce common patterns and rules.
/// </summary>
public class ProductImageRepository : FileStorageRepositoryBase<Product, ProductImage, CatalogDbContext>
{
    // Configure allowed image types
    private static readonly HashSet<string> _allowedContentTypes = new()
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    protected override HashSet<string> AllowedContentTypes => _allowedContentTypes;

    // Max file size: 10MB (using default from base class)
    protected override long MaxFileSizeBytes => 10 * 1024 * 1024;

    // Storage prefix for product images
    protected override string StoragePrefix => "products";

    // Content type to extension mapping
    private static readonly Dictionary<string, string[]> _contentTypeExtensionMap = new()
    {
        { "image/jpeg", new[] { ".jpg", ".jpeg" } },
        { "image/png", new[] { ".png" } },
        { "image/webp", new[] { ".webp" } },
        { "image/gif", new[] { ".gif" } }
    };

    protected override Dictionary<string, string[]> ContentTypeExtensionMap => _contentTypeExtensionMap;

    public ProductImageRepository(
        CatalogDbContext dbContext,
        IObjectStorageService storageService,
        ILogger<ProductImageRepository> logger)
        : base(dbContext, storageService, logger)
    {
    }

    #region Abstract Method Implementations

    protected override async Task ValidateEntityExistsAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var productExists = await DbContext.Products
            .AnyAsync(p => p.Id == entityId, cancellationToken);

        if (!productExists)
        {
            throw new InvalidOperationException($"Product with ID {entityId} not found");
        }
    }

    protected override ProductImage CreateFileEntity(
        Guid entityId,
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes,
        Dictionary<string, object> metadata)
    {
        var altText = metadata.GetValueOrDefault("altText") as string;
        var displayOrder = metadata.GetValueOrDefault("displayOrder") as int? ?? 0;
        var isPrimary = metadata.GetValueOrDefault("isPrimary") as bool? ?? false;

        return new ProductImage(
            productId: entityId,
            objectKey: objectKey,
            fileName: fileName,
            contentType: contentType,
            sizeInBytes: sizeInBytes,
            altText: altText,
            displayOrder: displayOrder,
            isPrimary: isPrimary);
    }

    protected override async Task<ProductImage?> GetFileEntityByIdAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await DbContext.ProductImages
            .FirstOrDefaultAsync(pi => pi.Id == fileId, cancellationToken);
    }

    protected override async Task<List<ProductImage>> GetFileEntitiesByEntityIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        return await DbContext.ProductImages
            .Where(pi => pi.ProductId == entityId)
            .OrderBy(pi => pi.DisplayOrder)
            .ThenBy(pi => pi.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    protected override Task AddFileEntityAsync(ProductImage fileEntity, CancellationToken cancellationToken)
    {
        DbContext.ProductImages.Add(fileEntity);
        return Task.CompletedTask;
    }

    protected override Task RemoveFileEntityAsync(ProductImage fileEntity, CancellationToken cancellationToken)
    {
        DbContext.ProductImages.Remove(fileEntity);
        return Task.CompletedTask;
    }

    protected override Task RemoveFileEntitiesAsync(List<ProductImage> fileEntities, CancellationToken cancellationToken)
    {
        DbContext.ProductImages.RemoveRange(fileEntities);
        return Task.CompletedTask;
    }

    protected override Guid GetFileEntityId(ProductImage fileEntity) => fileEntity.Id;

    protected override void UpdateFileEntityMetadata(ProductImage fileEntity, Dictionary<string, object> metadata)
    {
        var altText = metadata.GetValueOrDefault("altText") as string;
        var displayOrder = metadata.GetValueOrDefault("displayOrder") as int?;
        var isPrimary = metadata.GetValueOrDefault("isPrimary") as bool?;

        fileEntity.UpdateMetadata(altText, displayOrder, isPrimary);
    }

    #endregion

    #region Virtual Method Overrides

    protected override Dictionary<string, object> ExtractMetadata(Dictionary<string, string>? metadata)
    {
        var result = new Dictionary<string, object>();

        if (metadata == null)
        {
            return result;
        }

        // Extract altText
        if (metadata.TryGetValue("altText", out var altText))
        {
            result["altText"] = altText;
        }

        // Extract displayOrder
        if (metadata.TryGetValue("displayOrder", out var displayOrderStr) &&
            int.TryParse(displayOrderStr, out var displayOrder))
        {
            result["displayOrder"] = displayOrder;
        }
        else
        {
            result["displayOrder"] = 0;
        }

        // Extract isPrimary
        if (metadata.TryGetValue("isPrimary", out var isPrimaryStr) &&
            bool.TryParse(isPrimaryStr, out var isPrimary))
        {
            result["isPrimary"] = isPrimary;
        }
        else
        {
            result["isPrimary"] = false;
        }

        return result;
    }

    protected override async Task BeforeCreateFileEntityAsync(
        Guid entityId,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken)
    {
        var isPrimary = metadata.GetValueOrDefault("isPrimary") as bool? ?? false;

        // If this is primary, unset other primary images for this product
        if (isPrimary)
        {
            var existingPrimaryImages = await DbContext.ProductImages
                .Where(pi => pi.ProductId == entityId && pi.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var img in existingPrimaryImages)
            {
                img.UnmarkAsPrimary();
            }
        }
    }

    protected override async Task BeforeUpdateFileMetadataAsync(
        ProductImage fileEntity,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken)
    {
        var isPrimary = metadata.GetValueOrDefault("isPrimary") as bool?;

        // If setting as primary, unset other primary images for this product
        if (isPrimary == true && !fileEntity.IsPrimary)
        {
            var otherPrimaryImages = await DbContext.ProductImages
                .Where(pi => pi.ProductId == fileEntity.ProductId &&
                            pi.Id != fileEntity.Id &&
                            pi.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var img in otherPrimaryImages)
            {
                img.UnmarkAsPrimary();
            }
        }
    }

    protected override FileMetadataDto MapToFileMetadataDto(ProductImage fileEntity, string url)
    {
        return new FileMetadataDto
        {
            Id = fileEntity.Id,
            Url = url,
            FileName = fileEntity.FileName,
            ContentType = fileEntity.ContentType,
            SizeInBytes = fileEntity.SizeInBytes,
            UploadedAt = fileEntity.CreatedAt,
            DisplayOrder = fileEntity.DisplayOrder,
            Metadata = new Dictionary<string, string>
            {
                { "altText", fileEntity.AltText ?? string.Empty },
                { "isPrimary", fileEntity.IsPrimary.ToString() }
            }
        };
    }

    #endregion
}
