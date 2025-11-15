using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.DTOs;

namespace R2.ShopNet.Framework.Persistence.Storage;

/// <summary>
/// Generic base repository for managing file entities with MinIO storage.
/// Enforces common patterns and rules for file upload, validation, and management.
/// </summary>
/// <typeparam name="TEntity">The parent entity type (e.g., Product, User)</typeparam>
/// <typeparam name="TFileEntity">The file entity type that inherits from FileEntity (e.g., ProductImage)</typeparam>
/// <typeparam name="TDbContext">The DbContext type</typeparam>
public abstract class FileStorageRepositoryBase<TEntity, TFileEntity, TDbContext> : IMinIORepository<TEntity>
    where TEntity : class
    where TFileEntity : FileEntity
    where TDbContext : DbContext
{
    protected readonly TDbContext DbContext;
    protected readonly IObjectStorageService StorageService;
    protected readonly ILogger Logger;

    /// <summary>
    /// Allowed content types for file uploads (e.g., image types, document types).
    /// Override in derived class to customize.
    /// </summary>
    protected abstract HashSet<string> AllowedContentTypes { get; }

    /// <summary>
    /// Maximum file size in bytes.
    /// Override in derived class to customize. Default is 10MB.
    /// </summary>
    protected virtual long MaxFileSizeBytes => 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Storage prefix for organizing files (e.g., "products", "users").
    /// Override in derived class to customize.
    /// </summary>
    protected abstract string StoragePrefix { get; }

    /// <summary>
    /// Mapping of content types to valid file extensions.
    /// Override in derived class to customize.
    /// </summary>
    protected virtual Dictionary<string, string[]> ContentTypeExtensionMap => new();

    protected FileStorageRepositoryBase(
        TDbContext dbContext,
        IObjectStorageService storageService,
        ILogger logger)
    {
        DbContext = dbContext;
        StorageService = storageService;
        Logger = logger;
    }

    #region Public Interface Methods

    public async Task<string> GetDownloadUrlByObjectKeyAsync(
        string objectKey,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var url = await StorageService.GetPresignedUrlAsync(
            objectKey,
            expiryMinutes,
            cancellationToken);

        return url;
    }

    public async Task<FileMetadataDto> UploadFileAsync(
        Guid entityId,
        IFormFile file,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // Validate parent entity exists
        await ValidateEntityExistsAsync(entityId, cancellationToken);

        // Validate file
        ValidateFile(file);

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var prefix = $"{StoragePrefix}/{entityId}";

        // Upload to MinIO
        string objectKey;
        using (var stream = file.OpenReadStream())
        {
            objectKey = await StorageService.UploadAsync(
                stream,
                uniqueFileName,
                file.ContentType,
                prefix,
                cancellationToken);
        }

        // Extract and process metadata
        var extractedMetadata = ExtractMetadata(metadata);

        // Handle any pre-upload logic (e.g., unset primary flags)
        await BeforeCreateFileEntityAsync(entityId, extractedMetadata, cancellationToken);

        // Create file entity
        var fileEntity = CreateFileEntity(
            entityId,
            objectKey,
            file.FileName,
            file.ContentType,
            file.Length,
            extractedMetadata);

        // Add to DbContext and save
        await AddFileEntityAsync(fileEntity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Uploaded {FileEntityType} {FileId} for {EntityType} {EntityId} to {ObjectKey}",
            typeof(TFileEntity).Name,
            GetFileEntityId(fileEntity),
            typeof(TEntity).Name,
            entityId,
            objectKey);

        // Return metadata DTO
        return MapToFileMetadataDto(fileEntity, string.Empty);
    }

    public async Task<string> GetDownloadUrlAsync(
        Guid fileId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var fileEntity = await GetFileEntityByIdAsync(fileId, cancellationToken);

        if (fileEntity == null)
        {
            throw new InvalidOperationException($"{typeof(TFileEntity).Name} with ID {fileId} not found");
        }

        var url = await StorageService.GetPresignedUrlAsync(
            fileEntity.ObjectKey,
            expiryMinutes,
            cancellationToken);

        return url;
    }

    public async Task<List<FileMetadataDto>> GetFilesWithUrlsAsync(
        Guid entityId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var fileEntities = await GetFileEntitiesByEntityIdAsync(entityId, cancellationToken);

        var result = new List<FileMetadataDto>();

        foreach (var fileEntity in fileEntities)
        {
            var url = await StorageService.GetPresignedUrlAsync(
                fileEntity.ObjectKey,
                expiryMinutes,
                cancellationToken);

            result.Add(MapToFileMetadataDto(fileEntity, url));
        }

        return result;
    }

    public async Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var fileEntity = await GetFileEntityByIdAsync(fileId, cancellationToken);

        if (fileEntity == null)
        {
            Logger.LogWarning("{FileEntityType} {FileId} not found for deletion", typeof(TFileEntity).Name, fileId);
            return false;
        }

        // Delete from MinIO
        var deleted = await StorageService.DeleteAsync(
            fileEntity.ObjectKey,
            cancellationToken);

        if (!deleted)
        {
            Logger.LogWarning(
                "Failed to delete object {ObjectKey} from MinIO for {FileEntityType} {FileId}",
                fileEntity.ObjectKey,
                typeof(TFileEntity).Name,
                fileId);
        }

        // Delete from database (even if MinIO delete failed)
        await RemoveFileEntityAsync(fileEntity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Deleted {FileEntityType} {FileId} (ObjectKey: {ObjectKey})",
            typeof(TFileEntity).Name,
            fileId,
            fileEntity.ObjectKey);

        return true;
    }

    public async Task DeleteAllFilesAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var fileEntities = await GetFileEntitiesByEntityIdAsync(entityId, cancellationToken);

        foreach (var fileEntity in fileEntities)
        {
            await StorageService.DeleteAsync(fileEntity.ObjectKey, cancellationToken);
        }

        await RemoveFileEntitiesAsync(fileEntities, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Deleted {Count} {FileEntityType} for {EntityType} {EntityId}",
            fileEntities.Count,
            typeof(TFileEntity).Name,
            typeof(TEntity).Name,
            entityId);
    }

    public async Task UpdateFileMetadataAsync(
        Guid fileId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var fileEntity = await GetFileEntityByIdAsync(fileId, cancellationToken);

        if (fileEntity == null)
        {
            throw new InvalidOperationException($"{typeof(TFileEntity).Name} with ID {fileId} not found");
        }

        // Extract and process metadata
        var extractedMetadata = ExtractMetadata(metadata);

        // Handle any pre-update logic
        await BeforeUpdateFileMetadataAsync(fileEntity, extractedMetadata, cancellationToken);

        // Update the file entity metadata
        UpdateFileEntityMetadata(fileEntity, extractedMetadata);

        await DbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Updated metadata for {FileEntityType} {FileId}",
            typeof(TFileEntity).Name,
            fileId);
    }

    #endregion

    #region Abstract Methods (Must Override)

    /// <summary>
    /// Validate that the parent entity exists.
    /// Throw InvalidOperationException if not found.
    /// </summary>
    protected abstract Task ValidateEntityExistsAsync(Guid entityId, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new file entity with the provided data.
    /// </summary>
    protected abstract TFileEntity CreateFileEntity(
        Guid entityId,
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes,
        Dictionary<string, object> metadata);

    /// <summary>
    /// Get file entity by its ID.
    /// </summary>
    protected abstract Task<TFileEntity?> GetFileEntityByIdAsync(Guid fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Get all file entities for a parent entity, with sorting.
    /// </summary>
    protected abstract Task<List<TFileEntity>> GetFileEntitiesByEntityIdAsync(Guid entityId, CancellationToken cancellationToken);

    /// <summary>
    /// Add file entity to the DbContext.
    /// </summary>
    protected abstract Task AddFileEntityAsync(TFileEntity fileEntity, CancellationToken cancellationToken);

    /// <summary>
    /// Remove file entity from the DbContext.
    /// </summary>
    protected abstract Task RemoveFileEntityAsync(TFileEntity fileEntity, CancellationToken cancellationToken);

    /// <summary>
    /// Remove multiple file entities from the DbContext.
    /// </summary>
    protected abstract Task RemoveFileEntitiesAsync(List<TFileEntity> fileEntities, CancellationToken cancellationToken);

    /// <summary>
    /// Get the ID of a file entity.
    /// </summary>
    protected abstract Guid GetFileEntityId(TFileEntity fileEntity);

    /// <summary>
    /// Update file entity metadata (domain-specific properties).
    /// </summary>
    protected abstract void UpdateFileEntityMetadata(TFileEntity fileEntity, Dictionary<string, object> metadata);

    #endregion

    #region Virtual Methods (Can Override)

    /// <summary>
    /// Extract and parse metadata from the raw dictionary.
    /// Override to add custom metadata extraction logic.
    /// </summary>
    protected virtual Dictionary<string, object> ExtractMetadata(Dictionary<string, string>? metadata)
    {
        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Hook called before creating a file entity (e.g., to unset primary flags).
    /// </summary>
    protected virtual Task BeforeCreateFileEntityAsync(
        Guid entityId,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Hook called before updating file metadata.
    /// </summary>
    protected virtual Task BeforeUpdateFileMetadataAsync(
        TFileEntity fileEntity,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Map file entity to FileMetadataDto.
    /// Override to add custom metadata mapping.
    /// </summary>
    protected virtual FileMetadataDto MapToFileMetadataDto(TFileEntity fileEntity, string url)
    {
        return new FileMetadataDto
        {
            Id = GetFileEntityId(fileEntity),
            Url = url,
            FileName = fileEntity.FileName,
            ContentType = fileEntity.ContentType,
            SizeInBytes = fileEntity.SizeInBytes,
            UploadedAt = fileEntity.CreatedAt,
            Metadata = new Dictionary<string, string>()
        };
    }

    #endregion

    #region File Validation

    /// <summary>
    /// Validates file size, content type, and extension.
    /// </summary>
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

        var contentType = file.ContentType.ToLower();
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException(
                $"File type {file.ContentType} is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}");
        }

        // Validate file extension matches content type
        if (ContentTypeExtensionMap.Any())
        {
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (ContentTypeExtensionMap.TryGetValue(contentType, out var validExtensions) &&
                !validExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    $"File extension {extension} does not match content type {file.ContentType}");
            }
        }
    }

    #endregion
}
