using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace R2.ShopNet.Framework.Persistence.Storage.Abstractions;

/// <summary>
/// Low-level abstraction for object storage operations (S3-compatible).
/// This is the base MinIO client wrapper used by service-specific repositories.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Uploads a file to object storage
    /// </summary>
    /// <param name="fileStream">File content stream</param>
    /// <param name="fileName">Target file name (without path)</param>
    /// <param name="contentType">MIME type (e.g., "image/jpeg")</param>
    /// <param name="prefix">Optional folder prefix (e.g., "originals/", "thumbnails/")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Object key (full path in bucket)</returns>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from object storage
    /// </summary>
    /// <param name="objectKey">Object key (returned from UploadAsync)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content stream</returns>
    Task<Stream> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from object storage
    /// </summary>
    /// <param name="objectKey">Object key to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for temporary public access
    /// </summary>
    /// <param name="objectKey">Object key</param>
    /// <param name="expiryMinutes">URL expiry time in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned URL (valid for specified duration)</returns>
    Task<string> GetPresignedUrlAsync(
        string objectKey,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an object exists
    /// </summary>
    /// <param name="objectKey">Object key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all objects with a prefix
    /// </summary>
    /// <param name="prefix">Prefix filter (e.g., "product-123/")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of object keys</returns>
    Task<List<string>> ListAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an object to a new location
    /// </summary>
    /// <param name="sourceKey">Source object key</param>
    /// <param name="destinationKey">Destination object key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CopyAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken = default);
}
