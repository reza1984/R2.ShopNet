namespace R2.ShopNet.Framework.Persistence.Storage.DTOs;

/// <summary>
/// Standard DTO for file metadata returned by IMinIORepository
/// </summary>
public class FileMetadataDto
{
    /// <summary>
    /// File ID from database
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Presigned download URL (valid for specified duration)
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (e.g., "image/jpeg")
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Display order (if applicable)
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Additional metadata (service-specific)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
