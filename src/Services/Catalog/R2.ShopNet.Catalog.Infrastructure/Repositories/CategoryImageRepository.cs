using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Persistence.Storage;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.DTOs;

namespace R2.ShopNet.Catalog.Infrastructure.Repositories;

/// <summary>
/// Repository for managing category images in MinIO storage.
/// Inherits from FileStorageRepositoryBase to enforce common patterns and rules.
/// </summary>
public class CategoryImageRepository : FileStorageRepositoryBase<Category, CategoryImage, CatalogDbContext>
{
    // Configure allowed image types
    private static readonly HashSet<string> _allowedContentTypes = new()
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif",
        "image/svg+xml"
    };

    protected override HashSet<string> AllowedContentTypes => _allowedContentTypes;

    // Max file size: 5MB for category images
    protected override long MaxFileSizeBytes => 5 * 1024 * 1024;

    // Storage prefix for category images
    protected override string StoragePrefix => "categories";

    // Content type to extension mapping
    private static readonly Dictionary<string, string[]> _contentTypeExtensionMap = new()
    {
        { "image/jpeg", new[] { ".jpg", ".jpeg" } },
        { "image/png", new[] { ".png" } },
        { "image/webp", new[] { ".webp" } },
        { "image/gif", new[] { ".gif" } },
        { "image/svg+xml", new[] { ".svg" } }
    };

    protected override Dictionary<string, string[]> ContentTypeExtensionMap => _contentTypeExtensionMap;

    public CategoryImageRepository(
        CatalogDbContext dbContext,
        IObjectStorageService storageService,
        ILogger<CategoryImageRepository> logger)
        : base(dbContext, storageService, logger)
    {
    }

    #region Abstract Method Implementations

    protected override async Task ValidateEntityExistsAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var categoryExists = await DbContext.Categories
            .AnyAsync(c => c.Id == entityId, cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException($"Category with ID {entityId} not found");
        }
    }

    protected override CategoryImage CreateFileEntity(
        Guid entityId,
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes,
        Dictionary<string, object> metadata)
    {
        var altText = metadata.GetValueOrDefault("altText") as string;

        return new CategoryImage(
            categoryId: entityId,
            objectKey: objectKey,
            fileName: fileName,
            contentType: contentType,
            sizeInBytes: sizeInBytes,
            altText: altText);
    }

    protected override async Task<CategoryImage?> GetFileEntityByIdAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await DbContext.CategoryImages
            .FirstOrDefaultAsync(ci => ci.Id == fileId, cancellationToken);
    }

    protected override async Task<List<CategoryImage>> GetFileEntitiesByEntityIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        return await DbContext.CategoryImages
            .Where(ci => ci.CategoryId == entityId)
            .OrderBy(ci => ci.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    protected override Task AddFileEntityAsync(CategoryImage fileEntity, CancellationToken cancellationToken)
    {
        DbContext.CategoryImages.Add(fileEntity);
        return Task.CompletedTask;
    }

    protected override Task RemoveFileEntityAsync(CategoryImage fileEntity, CancellationToken cancellationToken)
    {
        DbContext.CategoryImages.Remove(fileEntity);
        return Task.CompletedTask;
    }

    protected override Task RemoveFileEntitiesAsync(List<CategoryImage> fileEntities, CancellationToken cancellationToken)
    {
        DbContext.CategoryImages.RemoveRange(fileEntities);
        return Task.CompletedTask;
    }

    protected override Guid GetFileEntityId(CategoryImage fileEntity) => fileEntity.Id;

    protected override void UpdateFileEntityMetadata(CategoryImage fileEntity, Dictionary<string, object> metadata)
    {
        var altText = metadata.GetValueOrDefault("altText") as string;
        fileEntity.UpdateMetadata(altText);
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

        return result;
    }

    protected override FileMetadataDto MapToFileMetadataDto(CategoryImage fileEntity, string url)
    {
        return new FileMetadataDto
        {
            Id = fileEntity.Id,
            Url = url,
            FileName = fileEntity.FileName,
            ContentType = fileEntity.ContentType,
            SizeInBytes = fileEntity.SizeInBytes,
            UploadedAt = fileEntity.CreatedAt,
            Metadata = new Dictionary<string, string>
            {
                { "altText", fileEntity.AltText ?? string.Empty }
            }
        };
    }

    #endregion
}
