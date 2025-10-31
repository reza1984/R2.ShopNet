namespace R2.ShopNet.Framework.Persistence.Storage.MinIO;

public class MinioOptions
{
    public const string SectionName = "MinIO";

    /// <summary>
    /// MinIO server endpoint (e.g., "localhost:9000" or "minio:9000")
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Service-specific access key (e.g., "catalog-service")
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Service-specific secret key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Bucket name for this service (e.g., "product-images")
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Use HTTPS (true for production, false for local development)
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Optional region (default: us-east-1)
    /// </summary>
    public string Region { get; set; } = "us-east-1";
}
