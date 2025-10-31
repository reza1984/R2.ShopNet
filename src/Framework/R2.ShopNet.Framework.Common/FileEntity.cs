using System;

namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Base entity for file storage with MinIO metadata.
/// Provides common properties for entities that store files in object storage.
/// </summary>
public abstract class FileEntity : AuditableSoftDeletableEntity
{
    /// <summary>
    /// MinIO object key (full path in bucket, e.g., "product-abc123/original-file.jpg")
    /// </summary>
    public string ObjectKey { get; protected set; } = string.Empty;

    /// <summary>
    /// Original filename when uploaded
    /// </summary>
    public string FileName { get; protected set; } = string.Empty;

    /// <summary>
    /// MIME content type (e.g., "image/jpeg", "image/png", "application/pdf")
    /// </summary>
    public string ContentType { get; protected set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeInBytes { get; protected set; }

    /// <summary>
    /// Constructor for EF Core
    /// </summary>
    protected FileEntity()
    {
    }

    /// <summary>
    /// Initialize file metadata
    /// </summary>
    protected void SetFileMetadata(
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes)
    {
        ObjectKey = objectKey ?? throw new ArgumentNullException(nameof(objectKey));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        SizeInBytes = sizeInBytes > 0 ? sizeInBytes : throw new ArgumentException("Size must be positive", nameof(sizeInBytes));
    }

    /// <summary>
    /// Update file metadata (e.g., when replacing a file)
    /// </summary>
    protected void UpdateFileMetadata(
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes)
    {
        ObjectKey = objectKey ?? throw new ArgumentNullException(nameof(objectKey));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        SizeInBytes = sizeInBytes > 0 ? sizeInBytes : throw new ArgumentException("Size must be positive", nameof(sizeInBytes));
    }
}
