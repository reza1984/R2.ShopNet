using Microsoft.AspNetCore.Http;

namespace R2.ShopNet.Framework.Persistence.Storage.Abstractions;

/// <summary>
/// High-level repository interface for MinIO file operations.
/// Each service implements this interface with domain-specific logic.
/// </summary>
/// <typeparam name="TEntity">The domain entity type (e.g., Product, User)</typeparam>
public interface IMinIORepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Upload a file and associate it with an entity
    /// </summary>
    /// <param name="entityId">Entity ID (e.g., ProductId)</param>
    /// <param name="file">File to upload</param>
    /// <param name="metadata">Optional metadata (e.g., altText, isPrimary)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata DTO</returns>
    Task<DTOs.FileMetadataDto> UploadFileAsync(
        Guid entityId,
        IFormFile file,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get presigned download URL for a file by its ObjectKey (does not query the database)
    /// </summary>
    /// <param name="objectKey">Object key in MinIO storage</param>
    /// <param name="expiryMinutes">URL expiry in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned URL</returns>
    Task<string> GetDownloadUrlByObjectKeyAsync(
        string objectKey,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get presigned download URL for a file by its file ID (queries the database)
    /// </summary>
    /// <param name="fileId">File ID from database</param>
    /// <param name="expiryMinutes">URL expiry in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned URL</returns>
    Task<string> GetDownloadUrlAsync(
        Guid fileId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all presigned URLs for an entity's files
    /// </summary>
    /// <param name="entityId">Entity ID</param>
    /// <param name="expiryMinutes">URL expiry in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of file metadata with presigned URLs</returns>
    Task<List<DTOs.FileMetadataDto>> GetFilesWithUrlsAsync(
        Guid entityId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file
    /// </summary>
    /// <param name="fileId">File ID from database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all files associated with an entity
    /// </summary>
    /// <param name="entityId">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAllFilesAsync(
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update file metadata (not the file content)
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="metadata">Metadata to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateFileMetadataAsync(
        Guid fileId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}
