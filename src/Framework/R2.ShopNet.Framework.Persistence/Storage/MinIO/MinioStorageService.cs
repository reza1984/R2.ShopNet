using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;

namespace R2.ShopNet.Framework.Persistence.Storage.MinIO;

public class MinioStorageService : IObjectStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IOptions<MinioOptions> options,
        ILogger<MinioStorageService> logger)
    {
        var config = options.Value;
        _bucketName = config.BucketName;
        _logger = logger;

        _minioClient = new MinioClient()
            .WithEndpoint(config.Endpoint)
            .WithCredentials(config.AccessKey, config.SecretKey)
            .WithSSL(config.UseSSL)
            .Build();

        _logger.LogInformation(
            "MinIO client initialized for bucket '{BucketName}' at '{Endpoint}'",
            _bucketName,
            config.Endpoint);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var objectKey = string.IsNullOrEmpty(prefix)
            ? fileName
            : $"{prefix.TrimEnd('/')}/{fileName}";

        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                // Cache images for 1 year (31536000 seconds)
                // Images are immutable (unique filenames with GUIDs)
                { "Cache-Control", "public, max-age=31536000, immutable" }
            };

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType)
                .WithHeaders(metadata);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Uploaded file '{ObjectKey}' to bucket '{BucketName}'",
                objectKey,
                _bucketName);

            return objectKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload file '{ObjectKey}' to bucket '{BucketName}'",
                objectKey,
                _bucketName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            memoryStream.Position = 0;

            _logger.LogInformation(
                "Downloaded file '{ObjectKey}' from bucket '{BucketName}'",
                objectKey,
                _bucketName);

            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to download file '{ObjectKey}' from bucket '{BucketName}'",
                objectKey,
                _bucketName);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Deleted file '{ObjectKey}' from bucket '{BucketName}'",
                objectKey,
                _bucketName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete file '{ObjectKey}' from bucket '{BucketName}'",
                objectKey,
                _bucketName);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(
        string objectKey,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithExpiry(expiryMinutes * 60); // Convert to seconds

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogInformation(
                "Generated presigned URL for '{ObjectKey}' (expires in {Expiry} minutes)",
                objectKey,
                expiryMinutes);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate presigned URL for '{ObjectKey}'",
                objectKey);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> ListAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var objectKeys = new List<string>();

        try
        {
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithPrefix(prefix ?? string.Empty)
                .WithRecursive(true);

            var observable = _minioClient.ListObjectsEnumAsync(listObjectsArgs, cancellationToken);

            await foreach (var item in observable)
            {
                objectKeys.Add(item.Key);
            }

            _logger.LogInformation(
                "Listed {Count} objects in bucket '{BucketName}' with prefix '{Prefix}'",
                objectKeys.Count,
                _bucketName,
                prefix ?? "(none)");

            return objectKeys;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to list objects in bucket '{BucketName}'",
                _bucketName);
            throw;
        }
    }

    public async Task CopyAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var copySourceObjectArgs = new CopySourceObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(sourceKey);

            var copyObjectArgs = new CopyObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(destinationKey)
                .WithCopyObjectSource(copySourceObjectArgs);

            await _minioClient.CopyObjectAsync(copyObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Copied file from '{SourceKey}' to '{DestinationKey}' in bucket '{BucketName}'",
                sourceKey,
                destinationKey,
                _bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to copy file from '{SourceKey}' to '{DestinationKey}'",
                sourceKey,
                destinationKey);
            throw;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(_bucketName);

        bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!found)
        {
            var makeBucketArgs = new MakeBucketArgs()
                .WithBucket(_bucketName);

            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);

            _logger.LogInformation(
                "Created bucket '{BucketName}'",
                _bucketName);
        }
    }
}
