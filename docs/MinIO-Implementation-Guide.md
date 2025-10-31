# MinIO Object Storage Implementation Guide

This guide provides a comprehensive, reusable pattern for implementing MinIO-based file storage across all microservices in the R2.ShopNet platform. Follow these patterns to ensure consistency, security, and maintainability.

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture & Patterns](#2-architecture--patterns)
3. [Infrastructure Setup](#3-infrastructure-setup)
4. [Framework Storage Abstraction](#4-framework-storage-abstraction)
5. [Service Integration](#5-service-integration)
6. [Integration with Product Handlers](#6-integration-with-product-handlers)
7. [CQRS Commands and Queries](#7-cqrs-commands-and-queries)
8. [Security & Access Control](#8-security--access-control)
   - [8.6 MinIO Console Access for Users](#86-minio-console-access-for-users)
9. [Code Examples](#9-code-examples)
10. [Best Practices](#10-best-practices)
11. [Troubleshooting](#11-troubleshooting)

---

## 1. Overview

### What is MinIO?

MinIO is an S3-compatible object storage system designed for self-hosted/on-premises deployments. In R2.ShopNet, MinIO serves as the centralized file storage solution for:

- **Product Images** (Catalog Service)
- **User Avatars** (Identity Service)
- **Documents & Attachments** (Document Service)
- **Media Files** (Media Service)
- **Any binary files** (Future Services)

### Why MinIO for R2.ShopNet?

✅ **S3-Compatible**: Standard API, easy migration to AWS/Azure later
✅ **Self-Hosted**: Aligns with on-premises deployment requirement
✅ **Scalable**: Supports distributed mode for high availability
✅ **Feature-Rich**: Bucket policies, versioning, encryption, events
✅ **Performant**: Optimized for large files and high throughput
✅ **Cost-Effective**: No cloud vendor costs or egress fees

### When to Use MinIO

**Use MinIO for:**
- Images (product photos, avatars, thumbnails)
- Documents (PDFs, Word files, spreadsheets)
- Media files (videos, audio)
- Archives (backups, exports)
- Any binary data > 1MB

**Don't use MinIO for:**
- Configuration data (use Consul KV)
- Session data (use Redis)
- Structured data (use PostgreSQL)
- Small metadata (< 1KB, store in database)

---

## 2. Architecture & Patterns

### Overall Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend Clients                     │
│         (Admin Portal, Public Website, Mobile)          │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP
                         ↓
┌─────────────────────────────────────────────────────────┐
│                  API Gateway (YARP)                     │
│            Routes requests via Consul                    │
└────────────────────────┬────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          ↓              ↓              ↓
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Catalog  │   │ Identity │   │  Media   │
    │ Service  │   │ Service  │   │ Service  │
    └────┬─────┘   └────┬─────┘   └────┬─────┘
         │              │              │
         │ Uses service-specific credentials
         │              │              │
         └──────────────┼──────────────┘
                        ↓
         ┌─────────────────────────────────┐
         │        MinIO Server             │
         │                                 │
         │  ┌────────────────────────┐    │
         │  │ product-images bucket  │    │ ← Catalog only
         │  └────────────────────────┘    │
         │  ┌────────────────────────┐    │
         │  │ user-avatars bucket    │    │ ← Identity only
         │  └────────────────────────┘    │
         │  ┌────────────────────────┐    │
         │  │ media-files bucket     │    │ ← Media only
         │  └────────────────────────┘    │
         └─────────────────────────────────┘
```

### Key Principles

1. **Service Ownership**: Each service owns its bucket(s) with exclusive access
2. **Gateway Pattern**: All file operations go through service APIs (never direct MinIO access)
3. **Access Control**: Service-specific credentials with scoped IAM policies
4. **Abstraction Layer**: `IObjectStorageService` interface hides MinIO implementation
5. **CQRS Pattern**: Commands for uploads/deletes, queries for downloads/lists

### Storage Flow Pattern

```
Upload Flow:
Client → Gateway → Service API → Validate → IObjectStorageService → MinIO
                                    ↓
                              Save metadata to PostgreSQL

Download Flow:
Client → Gateway → Service API → Generate Presigned URL → Return URL
Client → → → → → → → → → → → → → → → → → → → → MinIO (direct download)
```

---

## 3. Infrastructure Setup

### 3.1 Docker Compose Configuration

MinIO is already configured in `docker-compose.yml`. Here's how to set up service-specific access:

#### Update minio-setup Service

```yaml
minio-setup:
  image: minio/mc:latest
  container_name: shopnet-minio-setup
  depends_on:
    - minio
  entrypoint: >
    /bin/sh -c "
    sleep 10;
    /usr/bin/mc alias set myminio http://minio:9000 minioadmin minioadmin;

    # Create buckets with versioning
    /usr/bin/mc mb myminio/product-images --ignore-existing;
    /usr/bin/mc version enable myminio/product-images;

    /usr/bin/mc mb myminio/user-avatars --ignore-existing;
    /usr/bin/mc version enable myminio/user-avatars;

    /usr/bin/mc mb myminio/media-files --ignore-existing;
    /usr/bin/mc version enable myminio/media-files;

    # Create service users
    /usr/bin/mc admin user add myminio catalog-service ${CATALOG_MINIO_PASSWORD};
    /usr/bin/mc admin user add myminio identity-service ${IDENTITY_MINIO_PASSWORD};
    /usr/bin/mc admin user add myminio media-service ${MEDIA_MINIO_PASSWORD};

    # Create and attach policies (includes console access)
    echo '{
      \"Version\": \"2012-10-17\",
      \"Statement\": [{
        \"Effect\": \"Allow\",
        \"Action\": [
          \"s3:GetObject\",
          \"s3:PutObject\",
          \"s3:DeleteObject\",
          \"s3:ListBucket\",
          \"s3:GetBucketLocation\",
          \"s3:ListBucketMultipartUploads\",
          \"s3:ListMultipartUploadParts\",
          \"s3:AbortMultipartUpload\",
          \"s3:GetBucketPolicy\",
          \"s3:PutBucketPolicy\",
          \"s3:DeleteBucketPolicy\"
        ],
        \"Resource\": [\"arn:aws:s3:::product-images\", \"arn:aws:s3:::product-images/*\"]
      }]
    }' > /tmp/catalog-policy.json;
    /usr/bin/mc admin policy create myminio catalog-full-access /tmp/catalog-policy.json;
    /usr/bin/mc admin policy attach myminio catalog-full-access --user=catalog-service;

    echo '{
      \"Version\": \"2012-10-17\",
      \"Statement\": [{
        \"Effect\": \"Allow\",
        \"Action\": [
          \"s3:GetObject\",
          \"s3:PutObject\",
          \"s3:DeleteObject\",
          \"s3:ListBucket\",
          \"s3:GetBucketLocation\",
          \"s3:ListBucketMultipartUploads\",
          \"s3:ListMultipartUploadParts\",
          \"s3:AbortMultipartUpload\",
          \"s3:GetBucketPolicy\",
          \"s3:PutBucketPolicy\",
          \"s3:DeleteBucketPolicy\"
        ],
        \"Resource\": [\"arn:aws:s3:::user-avatars\", \"arn:aws:s3:::user-avatars/*\"]
      }]
    }' > /tmp/identity-policy.json;
    /usr/bin/mc admin policy create myminio identity-full-access /tmp/identity-policy.json;
    /usr/bin/mc admin policy attach myminio identity-full-access --user=identity-service;

    echo '{
      \"Version\": \"2012-10-17\",
      \"Statement\": [{
        \"Effect\": \"Allow\",
        \"Action\": [
          \"s3:GetObject\",
          \"s3:PutObject\",
          \"s3:DeleteObject\",
          \"s3:ListBucket\",
          \"s3:GetBucketLocation\",
          \"s3:ListBucketMultipartUploads\",
          \"s3:ListMultipartUploadParts\",
          \"s3:AbortMultipartUpload\",
          \"s3:GetBucketPolicy\",
          \"s3:PutBucketPolicy\",
          \"s3:DeleteBucketPolicy\"
        ],
        \"Resource\": [\"arn:aws:s3:::media-files\", \"arn:aws:s3:::media-files/*\"]
      }]
    }' > /tmp/media-policy.json;
    /usr/bin/mc admin policy create myminio media-full-access /tmp/media-policy.json;
    /usr/bin/mc admin policy attach myminio media-full-access --user=media-service;

    # DO NOT set public access - keep buckets private

    exit 0;
    "
  networks:
    - shopnet
```

#### Update .env File

Add MinIO passwords for each service:

```bash
# MinIO Root (Admin)
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=YourSecureMinioPassword123!

# Service-Specific MinIO Passwords
CATALOG_MINIO_PASSWORD=CatalogSecure123!
IDENTITY_MINIO_PASSWORD=IdentitySecure123!
MEDIA_MINIO_PASSWORD=MediaSecure123!
```

### 3.2 Bucket Naming Conventions

Follow these patterns for consistency:

| Service | Bucket Name | Purpose |
|---------|-------------|---------|
| Catalog | `product-images` | Product photos, thumbnails |
| Identity | `user-avatars` | User profile pictures |
| Media | `media-files` | Videos, audio, general media |
| Documents | `documents` | PDFs, Word files, archives |
| [YourService] | `[service]-files` | Service-specific files |

**Rules:**
- Use lowercase with hyphens
- Use plural forms
- Prefix with service name if ambiguous
- Keep under 63 characters

### 3.3 Service Account Pattern

For each new service that needs file storage:

1. **Create bucket** in minio-setup
2. **Add service user** with strong password
3. **Create IAM policy** granting full access to that bucket only
4. **Attach policy** to service user
5. **Store credentials** in service configuration

---

## 4. Framework Storage Abstraction

To maintain consistency across services, add storage abstractions to the existing `R2.ShopNet.Framework.Persistence` project.

### 4.1 Add Dependencies to Framework.Persistence

```bash
# Navigate to Framework.Persistence project
cd src/Framework/R2.ShopNet.Framework.Persistence

# Add MinIO package
dotnet add package Minio --version 6.0.3

# Microsoft.AspNetCore.Http.Features for IFormFile (if not already present)
dotnet add package Microsoft.AspNetCore.Http.Features --version 8.0.0
```

**Project Structure:**
```
R2.ShopNet.Framework.Persistence/
├── Storage/
│   ├── Abstractions/
│   │   ├── IObjectStorageService.cs
│   │   └── IMinIORepository.cs
│   ├── DTOs/
│   │   └── FileMetadataDto.cs
│   ├── MinIO/
│   │   ├── MinioStorageService.cs
│   │   └── MinioOptions.cs
│   └── Extensions/
│       └── StorageServiceCollectionExtensions.cs
├── UnitOfWork/
│   └── ... (existing UnitOfWork code)
└── ... (other existing code)
```

### 4.2 Storage Interfaces

#### IObjectStorageService (Low-Level)

Create `Storage/Abstractions/IObjectStorageService.cs` - This is the low-level MinIO client wrapper:

```csharp
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
```

#### IMinIORepository<TEntity> (Service-Specific)

Create `Storage/Abstractions/IMinIORepository.cs` - This is the high-level repository interface that each service implements:

```csharp
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
    Task<FileMetadataDto> UploadFileAsync(
        Guid entityId,
        IFormFile file,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get presigned download URL for a file
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
    Task<List<FileMetadataDto>> GetFilesWithUrlsAsync(
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
```

#### FileMetadataDto

Create `Storage/DTOs/FileMetadataDto.cs` - Common DTO for file information:

```csharp
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
```

### 4.3 MinioStorageService Implementation

Create `Storage/MinIO/MinioStorageService.cs`:

```csharp
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

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

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

            var observable = _minioClient.ListObjectsAsync(listObjectsArgs, cancellationToken);

            await foreach (var item in observable.WithCancellation(cancellationToken))
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
```

### 4.4 Configuration Options

Create `Storage/MinIO/MinioOptions.cs`:

```csharp
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
```

### 4.5 Dependency Injection Extensions

Create `Storage/Extensions/StorageServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.MinIO;

namespace R2.ShopNet.Framework.Persistence.Storage.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MinIO-based object storage service to DI container
    /// </summary>
    public static IServiceCollection AddMinioObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<MinioOptions>(
            configuration.GetSection(MinioOptions.SectionName));

        // Register storage service
        services.AddScoped<IObjectStorageService, MinioStorageService>();

        return services;
    }

    /// <summary>
    /// Adds MinIO-based object storage with explicit options
    /// </summary>
    public static IServiceCollection AddMinioObjectStorage(
        this IServiceCollection services,
        Action<MinioOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddScoped<IObjectStorageService, MinioStorageService>();

        return services;
    }
}
```

---

## 5. Service Integration

This section shows how to integrate the storage framework into any microservice (using Catalog service as an example).

### 5.1 Add Framework Reference

The Catalog.Infrastructure project should already reference Framework.Persistence:

```bash
cd src/Services/Catalog/R2.ShopNet.Catalog.Infrastructure

# Verify Framework.Persistence reference exists
dotnet list reference

# If not present, add it:
dotnet add reference ../../../Framework/R2.ShopNet.Framework.Persistence/R2.ShopNet.Framework.Persistence.csproj
```

**Note**: No additional project reference needed! Storage abstractions are now part of Framework.Persistence.

### 5.2 Update appsettings.json

Add MinIO configuration to `Catalog.API/appsettings.json`:

```json
{
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "catalog-service",
    "SecretKey": "CatalogSecure123!",
    "BucketName": "product-images",
    "UseSSL": false,
    "Region": "us-east-1"
  }
}
```

For production (`appsettings.Production.json`):

```json
{
  "MinIO": {
    "Endpoint": "minio:9000",
    "AccessKey": "catalog-service",
    "SecretKey": "${CATALOG_MINIO_PASSWORD}",
    "BucketName": "product-images",
    "UseSSL": true,
    "Region": "us-east-1"
  }
}
```

### 5.3 Register in Program.cs

Update `Catalog.API/Program.cs`:

```csharp
using R2.ShopNet.Framework.Persistence.Storage.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

// Add MinIO Object Storage
builder.Services.AddMinioObjectStorage(builder.Configuration);

// ... rest of configuration ...
```

### 5.4 Implement Service-Specific Repository

Each service creates its own implementation of `IMinIORepository<TEntity>` with domain-specific logic.

#### ProductImageMinIORepository (Catalog Service)

Create `Catalog.Infrastructure/Repositories/ProductImageMinIORepository.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Persistence.UnitOfWork;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.DTOs;

namespace R2.ShopNet.Catalog.Infrastructure.Repositories;

/// <summary>
/// Catalog-specific implementation of IMinIORepository for product images
/// </summary>
public class ProductImageMinIORepository : IMinIORepository<Product>
{
    private readonly IObjectStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductImageMinIORepository> _logger;

    public ProductImageMinIORepository(
        IObjectStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<ProductImageMinIORepository> logger)
    {
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FileMetadataDto> UploadFileAsync(
        Guid entityId,
        IFormFile file,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate product exists
        var productRepo = _unitOfWork.Repository<Product>();
        var product = await productRepo.AsQueryable()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == entityId && !p.IsDeleted, cancellationToken);

        if (product == null)
            throw new InvalidOperationException($"Product with ID '{entityId}' not found");

        // 2. Validate file
        var validationResult = ValidateImageFile(file);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(validationResult.Error);

        // 3. Generate unique filename
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";

        // 4. Upload to MinIO
        using var stream = file.OpenReadStream();
        var objectKey = await _storageService.UploadAsync(
            stream,
            fileName,
            file.ContentType,
            prefix: "originals",
            cancellationToken);

        // 5. Extract metadata
        var altText = metadata?.GetValueOrDefault("altText");
        var isPrimary = metadata?.GetValueOrDefault("isPrimary") == "true";

        // 6. Create ProductImage entity
        var imageUrl = $"minio://{objectKey}";
        var productImage = product.AddImage(imageUrl, altText, isPrimary);

        // 7. Save to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Uploaded image '{ObjectKey}' for product '{ProductId}'",
            objectKey,
            entityId);

        // 8. Return DTO with presigned URL
        var presignedUrl = await _storageService.GetPresignedUrlAsync(objectKey, 60, cancellationToken);

        return new FileMetadataDto
        {
            Id = productImage.Id,
            Url = presignedUrl,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeInBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            DisplayOrder = productImage.DisplayOrder,
            Metadata = new Dictionary<string, string>
            {
                ["altText"] = altText ?? "",
                ["isPrimary"] = isPrimary.ToString()
            }
        };
    }

    public async Task<string> GetDownloadUrlAsync(
        Guid fileId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        // 1. Get image from database
        var imageRepo = _unitOfWork.ReadOnlyRepository<ProductImage>();
        var image = await imageRepo.GetByIdAsync(fileId, cancellationToken);

        if (image == null)
            throw new InvalidOperationException($"Image with ID '{fileId}' not found");

        // 2. Extract object key from internal URL
        var objectKey = image.ImageUrl.Replace("minio://", "");

        // 3. Generate presigned URL
        return await _storageService.GetPresignedUrlAsync(objectKey, expiryMinutes, cancellationToken);
    }

    public async Task<List<FileMetadataDto>> GetFilesWithUrlsAsync(
        Guid entityId,
        int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        // 1. Get product with images
        var productRepo = _unitOfWork.ReadOnlyRepository<Product>();
        var product = await productRepo.AsQueryable()
            .Include(p => p.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == entityId && !p.IsDeleted, cancellationToken);

        if (product == null)
            return new List<FileMetadataDto>();

        // 2. Generate presigned URLs for all images
        var fileDtos = new List<FileMetadataDto>();

        foreach (var image in product.Images.OrderBy(i => i.DisplayOrder))
        {
            var objectKey = image.ImageUrl.Replace("minio://", "");
            var presignedUrl = await _storageService.GetPresignedUrlAsync(
                objectKey,
                expiryMinutes,
                cancellationToken);

            fileDtos.Add(new FileMetadataDto
            {
                Id = image.Id,
                Url = presignedUrl,
                FileName = Path.GetFileName(objectKey),
                ContentType = "image/jpeg", // Could store this in DB
                SizeInBytes = 0, // Could store this in DB
                UploadedAt = image.CreatedAt,
                DisplayOrder = image.DisplayOrder,
                Metadata = new Dictionary<string, string>
                {
                    ["altText"] = image.AltText ?? "",
                    ["isPrimary"] = image.IsPrimary.ToString()
                }
            });
        }

        return fileDtos;
    }

    public async Task<bool> DeleteFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get image and product
        var imageRepo = _unitOfWork.Repository<ProductImage>();
        var image = await imageRepo.AsQueryable()
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == fileId, cancellationToken);

        if (image == null)
            return false;

        // 2. Delete from MinIO
        var objectKey = image.ImageUrl.Replace("minio://", "");
        await _storageService.DeleteAsync(objectKey, cancellationToken);

        // 3. Remove from product
        image.Product.RemoveImage(fileId);

        // 4. Save to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted image '{ImageId}' (object key: '{ObjectKey}')",
            fileId,
            objectKey);

        return true;
    }

    public async Task DeleteAllFilesAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get product with images
        var productRepo = _unitOfWork.Repository<Product>();
        var product = await productRepo.AsQueryable()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == entityId && !p.IsDeleted, cancellationToken);

        if (product == null)
            return;

        // 2. Delete all images from MinIO
        foreach (var image in product.Images.ToList())
        {
            var objectKey = image.ImageUrl.Replace("minio://", "");
            await _storageService.DeleteAsync(objectKey, cancellationToken);

            _logger.LogInformation(
                "Deleted image '{ObjectKey}' from MinIO",
                objectKey);
        }

        // 3. Clear images from product
        product.Images.Clear();

        // 4. Save to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFileMetadataAsync(
        Guid fileId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        // 1. Get image
        var imageRepo = _unitOfWork.Repository<ProductImage>();
        var image = await imageRepo.GetByIdAsync(fileId, cancellationToken);

        if (image == null)
            throw new InvalidOperationException($"Image with ID '{fileId}' not found");

        // 2. Update metadata
        if (metadata.TryGetValue("altText", out var altText))
            image.UpdateAltText(altText);

        if (metadata.TryGetValue("displayOrder", out var displayOrder))
            image.UpdateDisplayOrder(int.Parse(displayOrder));

        if (metadata.TryGetValue("isPrimary", out var isPrimary) && bool.Parse(isPrimary))
            image.SetAsPrimary();

        // 3. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated metadata for image '{ImageId}'",
            fileId);
    }

    private (bool IsValid, string Error) ValidateImageFile(IFormFile file)
    {
        // Size check
        if (file.Length > 10_485_760) // 10MB
            return (false, "File size exceeds 10MB limit");

        // Type check
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return (false, "Invalid file type. Allowed: JPEG, PNG, WebP");

        // Extension check
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return (false, "Invalid file extension");

        return (true, string.Empty);
    }
}
```

#### Register Repository in DI

Update `Catalog.API/Program.cs`:

```csharp
using R2.ShopNet.Framework.Persistence.Storage.Extensions;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Catalog.Infrastructure.Repositories;
using R2.ShopNet.Catalog.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// ... existing registrations ...

// Add MinIO Object Storage (low-level)
builder.Services.AddMinioObjectStorage(builder.Configuration);

// Add Catalog-specific MinIO Repository (high-level)
builder.Services.AddScoped<IMinIORepository<Product>, ProductImageMinIORepository>();

// ... rest of configuration ...
```

### 5.5 Use in Application Layer

Inject `IMinIORepository<Product>` into command handlers:

```csharp
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.DTOs;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Catalog.Domain.Entities;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Handler that uses IMinIORepository for simplified file operations
/// </summary>
public class UploadProductImageCommandHandler
    : ICommandHandler<UploadProductImageCommand, Result<FileMetadataDto>>
{
    private readonly IMinIORepository<Product> _minioRepository;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    public UploadProductImageCommandHandler(
        IMinIORepository<Product> minioRepository,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _minioRepository = minioRepository;
        _logger = logger;
    }

    public async Task<Result<FileMetadataDto>> Handle(
        UploadProductImageCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Prepare metadata
            var metadata = new Dictionary<string, string>
            {
                ["altText"] = command.AltText ?? "",
                ["isPrimary"] = command.IsPrimary.ToString()
            };

            // Repository handles: validation, MinIO upload, entity creation, DB save
            var fileMetadata = await _minioRepository.UploadFileAsync(
                command.ProductId,
                command.File,
                metadata,
                cancellationToken);

            _logger.LogInformation(
                "Uploaded image '{FileId}' for product '{ProductId}'",
                fileMetadata.Id,
                command.ProductId);

            // Returns FileMetadataDto with presigned URL
            return Result.Success(fileMetadata);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<FileMetadataDto>(
                new Error("Product.ImageUploadFailed", ex.Message));
        }
    }
}
```

---

## 6. Integration with Product Handlers

**Important**: File operations are integrated **directly into product handlers** rather than having separate file endpoint controllers. This provides a cleaner API and better domain encapsulation.

### 6.1 Pattern Overview

```
Product Operations → Handler injects IMinIORepository<Product> → IObjectStorageService → MinIO
```

**Key Points:**
- Handlers inject `IMinIORepository<Product>` (not `IObjectStorageService` directly)
- Repository encapsulates domain logic, validation, and MinIO operations
- File uploads happen in `CreateProductCommandHandler` and `UpdateProductCommandHandler`
- File downloads (presigned URLs) happen in `GetProductByIdQueryHandler` and `GetProductsQueryHandler`
- File deletions happen in `DeleteProductCommandHandler`
- No separate `ProductImagesController` needed

**Architecture Layers:**
- **Handler** → Calls repository methods
- **IMinIORepository<Product>** → Domain-specific file operations
- **IObjectStorageService** → Low-level MinIO client wrapper

### 6.2 GetProductById with Presigned URLs

When retrieving a product, use the repository to generate presigned URLs:

```csharp
[GenerateHandler]
public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinIORepository<Product> _minioRepository; // Inject MinIO repository

    public GetProductByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMinIORepository<Product> minioRepository)
    {
        _unitOfWork = unitOfWork;
        _minioRepository = minioRepository;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.ReadOnlyRepository<Product>();
            var product = await repository.AsQueryable()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsNoTracking()
                .Where(p => p.Id == query.ProductId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                return Error.NotFound("Product.NotFound", $"Product with ID '{query.ProductId}' not found");
            }

            // Get images with presigned URLs from repository
            var imageFiles = await _minioRepository.GetFilesWithUrlsAsync(
                product.Id,
                expiryMinutes: 60,
                cancellationToken);

            // Map to DTO
            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price.Amount,
                // ... other fields ...

                // Images with presigned URLs
                Images = imageFiles.Select(f => new ProductImageDto
                {
                    Id = f.Id,
                    ImageUrl = f.Url, // Presigned URL from repository
                    AltText = f.Metadata.GetValueOrDefault("altText", ""),
                    DisplayOrder = f.DisplayOrder ?? 0,
                    IsPrimary = f.Metadata.GetValueOrDefault("isPrimary", "") == "True"
                }).ToList()
            };

            return Result<ProductDto>.Success(productDto);
        }
        catch (Exception ex)
        {
            return Error.Failure("Product.RetrievalFailed", $"Failed to retrieve product: {ex.Message}");
        }
    }
}
```

### 6.3 CreateProduct with File Upload

Handle file uploads directly in the create product handler:

```csharp
// Command includes file collection
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId,
    List<IFormFile>? Images = null, // Optional image files
    // ... other fields
) : ICommand<Result<ProductDto>>;

// Handler with file upload
[GenerateHandler]
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IObjectStorageService storageService,
        ILogger<CreateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate category exists
            var categoryRepo = _unitOfWork.ReadOnlyRepository<Category>();
            var category = await categoryRepo.GetByIdAsync(command.CategoryId, cancellationToken);
            if (category == null)
                return Error.NotFound("Category.NotFound", "Category not found");

            // 2. Create product entity
            var product = Product.Create(
                command.Name,
                command.Description,
                Money.Create(command.Price, "USD"),
                command.CategoryId
                // ... other fields
            );

            // 3. Upload images if provided
            if (command.Images != null && command.Images.Any())
            {
                foreach (var file in command.Images)
                {
                    // Validate file
                    var validationResult = FileValidation.ValidateImageFile(file);
                    if (validationResult.IsFailure)
                        return Result<ProductDto>.Failure(validationResult.Error);

                    // Generate unique filename
                    var extension = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid()}{extension}";

                    // Upload to MinIO
                    using var stream = file.OpenReadStream();
                    var objectKey = await _storageService.UploadAsync(
                        stream,
                        fileName,
                        file.ContentType,
                        prefix: "originals",
                        cancellationToken);

                    // Store internal reference in database
                    var imageUrl = $"minio://{objectKey}";
                    product.AddImage(imageUrl, altText: file.FileName);

                    _logger.LogInformation(
                        "Uploaded image '{ObjectKey}' for new product",
                        objectKey);
                }
            }

            // 4. Save to database
            var repository = _unitOfWork.Repository<Product>();
            await repository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Map to DTO (with presigned URLs)
            var productDto = await MapToDto(product, cancellationToken);

            return Result<ProductDto>.Success(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product");
            return Error.Failure("Product.CreationFailed", $"Failed to create product: {ex.Message}");
        }
    }

    private async Task<ProductDto> MapToDto(Product product, CancellationToken cancellationToken)
    {
        // Generate presigned URLs for response
        var imageDtos = new List<ProductImageDto>();
        foreach (var image in product.Images)
        {
            var objectKey = image.ImageUrl.Replace("minio://", "");
            var presignedUrl = await _storageService.GetPresignedUrlAsync(objectKey, 60, cancellationToken);

            imageDtos.Add(new ProductImageDto
            {
                Id = image.Id,
                ImageUrl = presignedUrl,
                AltText = image.AltText,
                IsPrimary = image.IsPrimary
            });
        }

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Images = imageDtos,
            // ... other fields
        };
    }
}
```

### 6.4 UpdateProduct with File Management

Handle image additions/removals in the update handler:

```csharp
public record UpdateProductCommand(
    Guid ProductId,
    string? Name = null,
    string? Description = null,
    List<IFormFile>? NewImages = null,
    List<Guid>? ImageIdsToRemove = null,
    // ... other fields
) : ICommand<Result<ProductDto>>;

[GenerateHandler]
public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Get product
        var repository = _unitOfWork.Repository<Product>();
        var product = await repository.AsQueryable()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId && !p.IsDeleted, cancellationToken);

        if (product == null)
            return Error.NotFound("Product.NotFound", "Product not found");

        // 2. Update basic fields
        if (command.Name != null)
            product.UpdateName(command.Name);
        if (command.Description != null)
            product.UpdateDescription(command.Description);

        // 3. Remove images if specified
        if (command.ImageIdsToRemove != null && command.ImageIdsToRemove.Any())
        {
            foreach (var imageId in command.ImageIdsToRemove)
            {
                var image = product.Images.FirstOrDefault(i => i.Id == imageId);
                if (image != null)
                {
                    // Delete from MinIO
                    var objectKey = image.ImageUrl.Replace("minio://", "");
                    await _storageService.DeleteAsync(objectKey, cancellationToken);

                    // Remove from product
                    product.RemoveImage(imageId);

                    _logger.LogInformation(
                        "Deleted image '{ImageId}' from product '{ProductId}'",
                        imageId,
                        command.ProductId);
                }
            }
        }

        // 4. Add new images if provided
        if (command.NewImages != null && command.NewImages.Any())
        {
            foreach (var file in command.NewImages)
            {
                var validationResult = FileValidation.ValidateImageFile(file);
                if (validationResult.IsFailure)
                    return Result<ProductDto>.Failure(validationResult.Error);

                var extension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";

                using var stream = file.OpenReadStream();
                var objectKey = await _storageService.UploadAsync(
                    stream,
                    fileName,
                    file.ContentType,
                    prefix: "originals",
                    cancellationToken);

                var imageUrl = $"minio://{objectKey}";
                product.AddImage(imageUrl, altText: file.FileName);
            }
        }

        // 5. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Return updated product with presigned URLs
        var productDto = await MapToDto(product, cancellationToken);
        return Result<ProductDto>.Success(productDto);
    }
}
```

### 6.5 DeleteProduct with Cleanup

Delete all associated files when deleting a product:

```csharp
[GenerateHandler]
public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public async Task<Result> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Get product with images
        var repository = _unitOfWork.Repository<Product>();
        var product = await repository.AsQueryable()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId && !p.IsDeleted, cancellationToken);

        if (product == null)
            return Error.NotFound("Product.NotFound", "Product not found");

        // 2. Delete all images from MinIO
        foreach (var image in product.Images)
        {
            var objectKey = image.ImageUrl.Replace("minio://", "");
            await _storageService.DeleteAsync(objectKey, cancellationToken);

            _logger.LogInformation(
                "Deleted image '{ObjectKey}' from MinIO",
                objectKey);
        }

        // 3. Soft delete product (or hard delete)
        product.MarkAsDeleted(); // Soft delete
        // OR: await repository.DeleteAsync(product, cancellationToken); // Hard delete

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted product '{ProductId}' with {ImageCount} images",
            command.ProductId,
            product.Images.Count);

        return Result.Success();
    }
}
```

### 6.6 Controller Integration

Your controller remains simple and delegates to handlers:

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;

    public ProductsController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
    }

    /// <summary>
    /// Get product by ID (images include presigned URLs)
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _queryDispatcher.Query(query);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Create product with optional images (multipart/form-data)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(52_428_800)] // 50MB for multiple images
    public async Task<ActionResult<ProductDto>> Create(
        [FromForm] CreateProductDto dto)
    {
        var command = new CreateProductCommand(
            dto.Name,
            dto.Description,
            dto.Price,
            dto.CategoryId,
            Request.Form.Files.ToList() // Pass uploaded files to handler
        );

        var result = await _commandDispatcher.Dispatch(command);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Update product (can add/remove images)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromForm] UpdateProductDto dto)
    {
        var command = new UpdateProductCommand(
            id,
            dto.Name,
            dto.Description,
            Request.Form.Files.ToList(), // New images
            dto.ImageIdsToRemove // Images to delete
        );

        var result = await _commandDispatcher.Dispatch(command);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete product (automatically deletes all images)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var command = new DeleteProductCommand(id);
        var result = await _commandDispatcher.Dispatch(command);

        if (result.IsFailure)
            return NotFound(result.Error);

        return NoContent();
    }
}
```

### 6.7 Benefits of This Approach

✅ **Single API Surface** - No separate `/api/products/{id}/images` endpoints needed
✅ **Domain Integrity** - File operations stay within product aggregate
✅ **Cleaner Code** - Handlers encapsulate all logic including file operations
✅ **Transaction Safety** - File operations and database updates in same handler
✅ **Better UX** - Create product with images in single request
✅ **Easier Testing** - Mock `IObjectStorageService` in handler unit tests

---

## 7. CQRS Commands and Queries

This section provides additional CQRS patterns for file operations that can be used alongside the handler integration shown in Section 6.

### 7.1 Commands

#### Upload Command

```csharp
using Microsoft.AspNetCore.Http;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Application.Commands;

public record UploadProductImageCommand(
    Guid ProductId,
    IFormFile File,
    string? AltText = null,
    bool IsPrimary = false
) : ICommand<Result<ProductImageDto>>;
```

#### Delete Command

```csharp
public record DeleteProductImageCommand(
    Guid ProductId,
    Guid ImageId
) : ICommand<Result>;
```

#### Update Command

```csharp
public record UpdateProductImageCommand(
    Guid ProductId,
    Guid ImageId,
    string? AltText,
    int DisplayOrder
) : ICommand<Result>;
```

### 7.2 Queries

#### Get Download URL Query

```csharp
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Application.Queries;

public record GetProductImageDownloadUrlQuery(
    Guid ProductId,
    Guid ImageId,
    int ExpiryMinutes = 60
) : IQuery<Result<string>>;
```

#### Query Handler

```csharp
public class GetProductImageDownloadUrlQueryHandler
    : IQueryHandler<GetProductImageDownloadUrlQuery, Result<string>>
{
    private readonly IProductRepository _productRepository;
    private readonly IObjectStorageService _storageService;

    public GetProductImageDownloadUrlQueryHandler(
        IProductRepository productRepository,
        IObjectStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService = storageService;
    }

    public async Task<Result<string>> Handle(
        GetProductImageDownloadUrlQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Get product with images
        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product == null)
            return Result.Failure<string>(new Error("Product.NotFound", "Product not found"));

        // 2. Find image
        var image = product.Images.FirstOrDefault(i => i.Id == query.ImageId);
        if (image == null)
            return Result.Failure<string>(new Error("ProductImage.NotFound", "Image not found"));

        // 3. Extract object key from internal URL (minio://object-key)
        var objectKey = image.ImageUrl.Replace("minio://", "");

        // 4. Generate presigned URL
        var presignedUrl = await _storageService.GetPresignedUrlAsync(
            objectKey,
            query.ExpiryMinutes,
            cancellationToken);

        return Result.Success(presignedUrl);
    }
}
```

### 7.3 Delete Handler with Cleanup

```csharp
public class DeleteProductImageCommandHandler
    : ICommandHandler<DeleteProductImageCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DeleteProductImageCommandHandler> _logger;

    public DeleteProductImageCommandHandler(
        IProductRepository productRepository,
        IObjectStorageService storageService,
        ILogger<DeleteProductImageCommandHandler> logger)
    {
        _productRepository = productRepository;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Get product
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result.Failure(new Error("Product.NotFound", "Product not found"));

        // 2. Find image
        var image = product.Images.FirstOrDefault(i => i.Id == command.ImageId);
        if (image == null)
            return Result.Failure(new Error("ProductImage.NotFound", "Image not found"));

        // 3. Remove from product
        product.RemoveImage(command.ImageId);

        // 4. Delete from MinIO
        var objectKey = image.ImageUrl.Replace("minio://", "");
        await _storageService.DeleteAsync(objectKey, cancellationToken);

        // 5. Save to database
        await _productRepository.UpdateAsync(product, cancellationToken);

        _logger.LogInformation(
            "Deleted image '{ImageId}' from product '{ProductId}'",
            command.ImageId,
            command.ProductId);

        return Result.Success();
    }
}
```

---

## 8. Security & Access Control

### 8.1 Service-Specific Credentials

Each service must use its own credentials and access ONLY its bucket:

```json
// Catalog Service
{
  "MinIO": {
    "AccessKey": "catalog-service",
    "SecretKey": "${CATALOG_MINIO_PASSWORD}",
    "BucketName": "product-images"
  }
}

// Identity Service
{
  "MinIO": {
    "AccessKey": "identity-service",
    "SecretKey": "${IDENTITY_MINIO_PASSWORD}",
    "BucketName": "user-avatars"
  }
}
```

**Never:**
- Share credentials between services
- Use root credentials (minioadmin) in services
- Hard-code passwords in appsettings.json
- Grant access to multiple buckets unless required

### 8.2 IAM Policy Template

Create this policy for each service (includes full console access):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject",
        "s3:ListBucket",
        "s3:GetBucketLocation",
        "s3:ListBucketMultipartUploads",
        "s3:ListMultipartUploadParts",
        "s3:AbortMultipartUpload",
        "s3:GetBucketPolicy",
        "s3:PutBucketPolicy",
        "s3:DeleteBucketPolicy"
      ],
      "Resource": [
        "arn:aws:s3:::{{BUCKET_NAME}}",
        "arn:aws:s3:::{{BUCKET_NAME}}/*"
      ]
    }
  ]
}
```

Replace `{{BUCKET_NAME}}` with your service's bucket (e.g., `product-images`).

**Permissions Explained:**
- `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject` - Basic file operations (API & Console)
- `s3:ListBucket`, `s3:GetBucketLocation` - List files and bucket info (Console navigation)
- `s3:ListBucketMultipartUploads`, `s3:ListMultipartUploadParts`, `s3:AbortMultipartUpload` - Large file uploads (Console upload UI)
- `s3:GetBucketPolicy`, `s3:PutBucketPolicy`, `s3:DeleteBucketPolicy` - Manage bucket policies (Console settings)

This policy grants **full access** to the specified bucket through both:
- ✅ Service API (programmatic access)
- ✅ MinIO Console UI (web portal management)

### 8.3 File Validation

Always validate files before upload:

```csharp
public static class FileValidation
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private static readonly string[] AllowedDocumentTypes = { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

    public static Result ValidateImageFile(IFormFile file)
    {
        // Size check
        if (file.Length > 10_485_760) // 10MB
            return Result.Failure(new Error("File.TooLarge", "File size exceeds 10MB"));

        // Type check
        if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
            return Result.Failure(new Error("File.InvalidType", "Invalid file type. Allowed: JPEG, PNG, WebP"));

        // Extension check
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return Result.Failure(new Error("File.InvalidExtension", "Invalid file extension"));

        // Magic bytes check (more secure)
        using var reader = new BinaryReader(file.OpenReadStream());
        var headerBytes = reader.ReadBytes(8);
        if (!IsValidImageHeader(headerBytes))
            return Result.Failure(new Error("File.InvalidContent", "File content does not match extension"));

        return Result.Success();
    }

    private static bool IsValidImageHeader(byte[] headerBytes)
    {
        // JPEG: FF D8 FF
        if (headerBytes.Length >= 3 &&
            headerBytes[0] == 0xFF &&
            headerBytes[1] == 0xD8 &&
            headerBytes[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (headerBytes.Length >= 8 &&
            headerBytes[0] == 0x89 &&
            headerBytes[1] == 0x50 &&
            headerBytes[2] == 0x4E &&
            headerBytes[3] == 0x47)
            return true;

        // WebP: 52 49 46 46 xx xx xx xx 57 45 42 50
        if (headerBytes.Length >= 4 &&
            headerBytes[0] == 0x52 &&
            headerBytes[1] == 0x49 &&
            headerBytes[2] == 0x46 &&
            headerBytes[3] == 0x46)
            return true;

        return false;
    }
}
```

### 8.4 Authorization Patterns

```csharp
// Only admin can upload/delete
[Authorize(Roles = "Admin")]
public async Task<ActionResult> UploadImage(...)

// Public can view/download
[AllowAnonymous]
public async Task<ActionResult> GetDownloadUrl(...)

// Owner can manage their own files
[Authorize]
public async Task<ActionResult> UploadAvatar(...)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // Validate ownership
}
```

### 8.5 Audit Logging

Log all file operations:

```csharp
_logger.LogInformation(
    "User {UserId} uploaded file '{FileName}' ({Size} bytes) for product {ProductId}",
    userId,
    fileName,
    file.Length,
    productId);

_logger.LogWarning(
    "Failed file upload attempt by user {UserId}: {Reason}",
    userId,
    validationResult.Error.Message);

_logger.LogInformation(
    "User {UserId} deleted file '{ObjectKey}' from product {ProductId}",
    userId,
    objectKey,
    productId);
```

### 8.6 MinIO Console Access for Users

Service accounts can access the MinIO Console (web portal) to manually manage files and directories.

#### Accessing the MinIO Console

**URL**: `http://localhost:9001` (development) or `https://minio.yourdomain.com:9001` (production)

**Login with Service Credentials:**
- **Username**: Service account name (e.g., `catalog-service`)
- **Password**: Service password (e.g., `CatalogSecure123!`)

#### What Users Can Do in the Console

With the full access policy configured above, users logged in with their service credentials can:

✅ **Browse Bucket**
- Navigate through folders (prefixes) like `originals/`, `thumbnails/`, `medium/`
- View file listings with metadata (size, last modified)
- Search and filter files

✅ **Upload Files**
- Drag-and-drop file uploads through web UI
- Multi-file uploads
- Large file uploads (multipart upload support)
- Create folders/prefixes

✅ **Download Files**
- Download individual files
- Preview images directly in browser
- Download multiple files

✅ **Delete Files**
- Delete individual files
- Delete folders and all contents
- Batch delete operations

✅ **Manage Metadata**
- View file properties (size, type, date)
- View and edit custom metadata tags
- Set content-type headers

✅ **Bucket Operations**
- View bucket policies
- Update bucket policies
- View bucket statistics and usage

#### Console Access Restrictions

Service accounts can **ONLY** access their assigned bucket:
- ❌ `catalog-service` **cannot** see `user-avatars` or `media-files` buckets
- ❌ `identity-service` **cannot** see `product-images` or `media-files` buckets
- ✅ Each service **can** see and manage **only their own bucket**

This is enforced by the IAM policy Resource restriction:
```json
"Resource": [
  "arn:aws:s3:::product-images",
  "arn:aws:s3:::product-images/*"
]
```

#### Example Console Workflow

**Scenario: Catalog team needs to manually upload product images**

1. Open browser to `http://localhost:9001`
2. Login with:
   - Username: `catalog-service`
   - Password: `CatalogSecure123!`
3. Click on `product-images` bucket (only bucket visible)
4. Navigate to `originals/` folder
5. Click "Upload" button
6. Drag-and-drop multiple product images
7. Files are uploaded with multipart support
8. View uploaded files in the console
9. Can download, delete, or preview images

#### Console UI Features

The MinIO Console provides:
- **Object Browser**: File manager interface with folder navigation
- **Upload Manager**: Progress tracking for uploads
- **Preview**: In-browser preview for images, PDFs, text files
- **Search**: Search files by name or prefix
- **Bulk Operations**: Select multiple files for batch actions
- **Bucket Info**: Usage statistics, file count, total size
- **Policy Editor**: JSON editor for bucket policies (if permissions granted)

#### Security Notes for Console Access

**Best Practices:**
1. ✅ Use service accounts (never root `minioadmin` account)
2. ✅ Use strong passwords for service accounts
3. ✅ Enable HTTPS for production console access
4. ✅ Restrict console access to internal network only
5. ✅ Audit console login attempts
6. ✅ Rotate service account passwords periodically

**Production Configuration:**
```yaml
# Restrict console to internal network only (Nginx)
location /minio-console/ {
    proxy_pass http://minio:9001/;

    # Allow only internal network
    allow 10.0.0.0/8;
    allow 172.16.0.0/12;
    allow 192.168.0.0/16;
    deny all;

    # Additional security headers
    add_header X-Frame-Options "DENY";
    add_header X-Content-Type-Options "nosniff";
}
```

#### Alternative: Read-Only Console Access

If you want users to only **view** files (not upload/delete) through the console:

```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "s3:GetObject",
      "s3:ListBucket",
      "s3:GetBucketLocation"
    ],
    "Resource": [
      "arn:aws:s3:::{{BUCKET_NAME}}",
      "arn:aws:s3:::{{BUCKET_NAME}}/*"
    ]
  }]
}
```

This allows:
- ✅ Browse and navigate folders
- ✅ Download files
- ✅ Preview files
- ❌ No upload capability
- ❌ No delete capability
- ❌ No policy modification

---

## 9. Code Examples

### 9.1 Complete Upload Flow

```csharp
// Command
public record UploadProductImageCommand(
    Guid ProductId,
    IFormFile File,
    string? AltText,
    bool IsPrimary
) : ICommand<Result<ProductImageDto>>;

// Handler
public class UploadProductImageCommandHandler
    : ICommandHandler<UploadProductImageCommand, Result<ProductImageDto>>
{
    private readonly IObjectStorageService _storageService;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    public UploadProductImageCommandHandler(
        IObjectStorageService storageService,
        IProductRepository productRepository,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _storageService = storageService;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<Result<ProductImageDto>> Handle(
        UploadProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate file
        var validationResult = FileValidation.ValidateImageFile(command.File);
        if (validationResult.IsFailure)
            return Result.Failure<ProductImageDto>(validationResult.Error);

        // 2. Validate product exists
        var product = await _productRepository.GetByIdAsync(
            command.ProductId,
            cancellationToken);

        if (product == null)
            return Result.Failure<ProductImageDto>(
                new Error("Product.NotFound", "Product not found"));

        // 3. Generate unique filename
        var extension = Path.GetExtension(command.File.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";

        // 4. Upload to MinIO
        using var stream = command.File.OpenReadStream();
        var objectKey = await _storageService.UploadAsync(
            stream,
            fileName,
            command.File.ContentType,
            prefix: "originals",
            cancellationToken);

        // 5. Create ProductImage entity
        var imageUrl = $"minio://{objectKey}";
        var productImage = product.AddImage(
            imageUrl,
            command.AltText,
            command.IsPrimary);

        // 6. Save to database
        await _productRepository.UpdateAsync(product, cancellationToken);

        _logger.LogInformation(
            "Uploaded image '{ObjectKey}' ({Size} bytes) for product '{ProductId}'",
            objectKey,
            command.File.Length,
            command.ProductId);

        // 7. Return DTO
        return Result.Success(new ProductImageDto
        {
            Id = productImage.Id,
            ImageUrl = imageUrl,
            AltText = productImage.AltText,
            DisplayOrder = productImage.DisplayOrder,
            IsPrimary = productImage.IsPrimary,
            CreatedAt = productImage.CreatedAt
        });
    }
}
```

### 9.2 Image Processing with Upload

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

public async Task<Result<ProductImageDto>> Handle(
    UploadProductImageCommand command,
    CancellationToken cancellationToken)
{
    // ... validation and product check ...

    var extension = Path.GetExtension(command.File.FileName);
    var baseFileName = Guid.NewGuid().ToString();

    // Upload original
    using var originalStream = command.File.OpenReadStream();
    var originalKey = await _storageService.UploadAsync(
        originalStream,
        $"{baseFileName}{extension}",
        command.File.ContentType,
        prefix: "originals",
        cancellationToken);

    // Generate and upload thumbnail (200x200)
    using var thumbnailStream = await GenerateThumbnail(
        command.File,
        200,
        200,
        cancellationToken);

    var thumbnailKey = await _storageService.UploadAsync(
        thumbnailStream,
        $"{baseFileName}{extension}",
        "image/jpeg",
        prefix: "thumbnails",
        cancellationToken);

    // Generate and upload medium size (800x800)
    using var mediumStream = await GenerateThumbnail(
        command.File,
        800,
        800,
        cancellationToken);

    var mediumKey = await _storageService.UploadAsync(
        mediumStream,
        $"{baseFileName}{extension}",
        "image/jpeg",
        prefix: "medium",
        cancellationToken);

    // Store all variants in database
    var imageUrl = $"minio://{originalKey}";
    var productImage = product.AddImage(imageUrl, command.AltText, command.IsPrimary);
    productImage.SetThumbnailUrl($"minio://{thumbnailKey}");
    productImage.SetMediumUrl($"minio://{mediumKey}");

    await _productRepository.UpdateAsync(product, cancellationToken);

    return Result.Success(MapToDto(productImage));
}

private async Task<Stream> GenerateThumbnail(
    IFormFile file,
    int width,
    int height,
    CancellationToken cancellationToken)
{
    using var image = await Image.LoadAsync(file.OpenReadStream(), cancellationToken);

    image.Mutate(x => x
        .Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));

    var memoryStream = new MemoryStream();
    await image.SaveAsJpegAsync(
        memoryStream,
        new JpegEncoder { Quality = 85 },
        cancellationToken);

    memoryStream.Position = 0;
    return memoryStream;
}
```

### 9.3 Bulk Delete on Product Deletion

```csharp
public class DeleteProductCommandHandler
    : ICommandHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public async Task<Result> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Get product with images
        var product = await _productRepository.GetByIdAsync(
            command.ProductId,
            cancellationToken);

        if (product == null)
            return Result.Failure(new Error("Product.NotFound", "Product not found"));

        // 2. Delete all images from MinIO
        foreach (var image in product.Images)
        {
            var objectKey = image.ImageUrl.Replace("minio://", "");
            await _storageService.DeleteAsync(objectKey, cancellationToken);

            // Also delete variants if they exist
            if (!string.IsNullOrEmpty(image.ThumbnailUrl))
            {
                var thumbnailKey = image.ThumbnailUrl.Replace("minio://", "");
                await _storageService.DeleteAsync(thumbnailKey, cancellationToken);
            }

            if (!string.IsNullOrEmpty(image.MediumUrl))
            {
                var mediumKey = image.MediumUrl.Replace("minio://", "");
                await _storageService.DeleteAsync(mediumKey, cancellationToken);
            }

            _logger.LogInformation(
                "Deleted image '{ObjectKey}' and variants",
                objectKey);
        }

        // 3. Delete product from database
        await _productRepository.DeleteAsync(product, cancellationToken);

        _logger.LogInformation(
            "Deleted product '{ProductId}' with {ImageCount} images",
            command.ProductId,
            product.Images.Count);

        return Result.Success();
    }
}
```

---

## 10. Best Practices

### 10.1 File Naming Conventions

**Always use:**
- GUID-based filenames (avoid user-provided names)
- Lowercase extensions
- Prefix folders for organization

```csharp
// Good
var fileName = $"{Guid.NewGuid()}.jpg";
var objectKey = $"originals/{fileName}";

// Bad
var fileName = command.File.FileName; // User-controlled, security risk
var objectKey = fileName; // No organization
```

### 10.2 Bucket Organization

Organize files with prefixes (folders):

```
product-images/
├── originals/
│   ├── 550e8400-e29b-41d4-a716-446655440000.jpg
│   └── 6ba7b810-9dad-11d1-80b4-00c04fd430c8.jpg
├── thumbnails/
│   ├── 550e8400-e29b-41d4-a716-446655440000.jpg
│   └── 6ba7b810-9dad-11d1-80b4-00c04fd430c8.jpg
├── medium/
│   └── ...
└── large/
    └── ...
```

### 10.3 URL Storage Pattern

**Don't store presigned URLs in database** (they expire).

**Instead:**
- Store internal reference: `minio://originals/guid.jpg`
- Generate presigned URLs on-demand
- Return presigned URLs in API responses

```csharp
// Database (permanent)
ImageUrl = "minio://originals/550e8400-e29b-41d4-a716-446655440000.jpg"

// API Response (temporary, generated on-demand)
{
  "imageUrl": "https://minio:9000/product-images/originals/550e...?X-Amz-Expires=3600&..."
}
```

### 10.4 Error Handling

```csharp
try
{
    await _storageService.UploadAsync(...);
}
catch (MinioException ex) when (ex.Message.Contains("Access Denied"))
{
    _logger.LogError(ex, "MinIO access denied - check credentials");
    return Result.Failure(new Error("Storage.AccessDenied", "Storage access denied"));
}
catch (MinioException ex) when (ex.Message.Contains("NoSuchBucket"))
{
    _logger.LogError(ex, "Bucket '{BucketName}' does not exist", bucketName);
    return Result.Failure(new Error("Storage.BucketNotFound", "Storage bucket not found"));
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to upload file to storage");
    return Result.Failure(new Error("Storage.UploadFailed", "Failed to upload file"));
}
```

### 10.5 Performance Optimization

**Use streaming for large files:**
```csharp
// Good - streams file (low memory)
using var stream = file.OpenReadStream();
await _storageService.UploadAsync(stream, ...);

// Bad - loads entire file into memory
var bytes = await file.GetBytes();
var memoryStream = new MemoryStream(bytes);
await _storageService.UploadAsync(memoryStream, ...);
```

**Use presigned URLs for downloads:**
```csharp
// Good - browser downloads directly from MinIO (fast)
var presignedUrl = await _storageService.GetPresignedUrlAsync(objectKey);
return Redirect(presignedUrl);

// Bad - proxies through API (slow, high memory)
var stream = await _storageService.DownloadAsync(objectKey);
return File(stream, "image/jpeg");
```

### 10.6 Cleanup Strategies

**Option 1: Immediate delete**
```csharp
// Delete from MinIO immediately when entity is deleted
await _storageService.DeleteAsync(objectKey);
```

**Option 2: Soft delete with background cleanup**
```csharp
// Mark as deleted, clean up later with background job
image.MarkAsDeleted();
await _productRepository.UpdateAsync(product);

// Background job (runs daily)
public async Task CleanupDeletedFilesAsync()
{
    var deletedImages = await _productRepository.GetDeletedImagesAsync();
    foreach (var image in deletedImages)
    {
        var objectKey = image.ImageUrl.Replace("minio://", "");
        await _storageService.DeleteAsync(objectKey);
        await _productRepository.PermanentlyDeleteImageAsync(image.Id);
    }
}
```

### 10.7 Testing

**Mock IObjectStorageService in unit tests:**
```csharp
[Fact]
public async Task Handle_ValidCommand_UploadsImage()
{
    // Arrange
    var mockStorage = new Mock<IObjectStorageService>();
    mockStorage
        .Setup(x => x.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync("originals/test-guid.jpg");

    var handler = new UploadProductImageCommandHandler(
        mockStorage.Object,
        mockProductRepository.Object,
        mockLogger.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    mockStorage.Verify(x => x.UploadAsync(
        It.IsAny<Stream>(),
        It.IsAny<string>(),
        "image/jpeg",
        "originals",
        It.IsAny<CancellationToken>()), Times.Once);
}
```

---

## 11. Troubleshooting

### 11.1 Common Issues

#### Issue: "Access Denied" Error

**Symptoms:**
```
Minio.Exceptions.AccessDeniedException: Access Denied
```

**Solutions:**
1. Verify service account exists:
   ```bash
   docker exec shopnet-minio mc admin user list myminio
   ```

2. Check policy attachment:
   ```bash
   docker exec shopnet-minio mc admin policy info myminio catalog-full-access
   ```

3. Verify credentials in appsettings.json match MinIO users

4. Ensure bucket name matches policy resource

#### Issue: "Bucket Not Found"

**Symptoms:**
```
Minio.Exceptions.BucketNotFoundException: Bucket 'product-images' does not exist
```

**Solutions:**
1. Check if bucket was created:
   ```bash
   docker exec shopnet-minio mc ls myminio
   ```

2. Verify minio-setup container ran successfully:
   ```bash
   docker logs shopnet-minio-setup
   ```

3. Manually create bucket:
   ```bash
   docker exec shopnet-minio mc mb myminio/product-images
   ```

#### Issue: "Connection Refused"

**Symptoms:**
```
System.Net.Http.HttpRequestException: Connection refused
```

**Solutions:**
1. Check MinIO container is running:
   ```bash
   docker ps | grep minio
   ```

2. Verify endpoint in appsettings.json:
   - Development: `localhost:9000`
   - Docker: `minio:9000`

3. Check network connectivity:
   ```bash
   docker exec shopnet-catalog-api ping minio
   ```

#### Issue: File Upload Fails Silently

**Symptoms:**
- No error, but file not in MinIO

**Solutions:**
1. Check stream position:
   ```csharp
   stream.Position = 0; // Reset before upload
   ```

2. Verify stream is not disposed:
   ```csharp
   using var stream = file.OpenReadStream();
   await _storageService.UploadAsync(stream, ...);
   // Stream is still open here
   ```

3. Enable MinIO SDK debug logging:
   ```csharp
   // In MinioStorageService constructor
   _minioClient.SetTraceOn();
   ```

#### Issue: Cannot Login to MinIO Console

**Symptoms:**
- "Invalid credentials" error on console login
- Can login with `minioadmin` but not with service account

**Solutions:**
1. Verify service account was created:
   ```bash
   docker exec shopnet-minio mc admin user list myminio
   # Should show: catalog-service, identity-service, media-service
   ```

2. Check if minio-setup ran successfully:
   ```bash
   docker logs shopnet-minio-setup
   # Look for "Added user successfully"
   ```

3. Manually create service account if missing:
   ```bash
   docker exec shopnet-minio mc admin user add myminio catalog-service CatalogSecure123!
   ```

4. Verify password matches environment variable in docker-compose

5. Try logging in with root account first (`minioadmin/minioadmin`) to verify console is working

#### Issue: Console Shows "No Buckets" After Login

**Symptoms:**
- Login successful but no buckets visible
- Service account can't see their bucket in console

**Solutions:**
1. Check policy is attached to user:
   ```bash
   docker exec shopnet-minio mc admin user info myminio catalog-service
   # Should show: Policy: catalog-full-access
   ```

2. Manually attach policy:
   ```bash
   docker exec shopnet-minio mc admin policy attach myminio catalog-full-access --user=catalog-service
   ```

3. Verify policy includes ListBucket action:
   ```bash
   docker exec shopnet-minio mc admin policy info myminio catalog-full-access
   ```

4. Check if bucket exists:
   ```bash
   docker exec shopnet-minio mc ls myminio
   ```

#### Issue: Console Upload Fails

**Symptoms:**
- Upload button visible but uploads fail
- "Permission denied" on upload

**Solutions:**
1. Verify policy includes PutObject action (see section 8.2)

2. Check policy includes multipart upload actions:
   - `s3:ListBucketMultipartUploads`
   - `s3:ListMultipartUploadParts`
   - `s3:AbortMultipartUpload`

3. Check file size limits (MinIO default is 5TB per file)

4. Check browser console for JavaScript errors

### 11.2 Health Checks

Add MinIO health check to your service:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<MinioHealthCheck>("minio");

// MinioHealthCheck.cs
public class MinioHealthCheck : IHealthCheck
{
    private readonly IObjectStorageService _storageService;

    public MinioHealthCheck(IObjectStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to list objects (simple connectivity test)
            await _storageService.ListAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MinIO is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "MinIO is not accessible",
                ex);
        }
    }
}
```

### 11.3 Debugging Tips

**View MinIO logs:**
```bash
docker logs shopnet-minio -f
```

**Access MinIO Console:**
```
http://localhost:9001
Username: minioadmin
Password: minioadmin
```

**List objects in bucket:**
```bash
docker exec shopnet-minio mc ls myminio/product-images --recursive
```

**Check bucket policy:**
```bash
docker exec shopnet-minio mc admin policy info myminio catalog-full-access
```

**Test presigned URL:**
```bash
curl -I "https://localhost:9000/product-images/originals/test.jpg?X-Amz-Expires=..."
```

### 11.4 Performance Issues

**Problem: Slow uploads**
- Use larger buffer size
- Enable compression for large files
- Check network bandwidth

**Problem: High memory usage**
- Stream files instead of loading into memory
- Process images in batches
- Implement file size limits

**Problem: Slow presigned URL generation**
- Cache presigned URLs (with expiry tracking)
- Use shorter expiry times
- Generate URLs in background job

---

## Quick Reference

### Creating New Service Integration

1. **Add Framework reference** to Infrastructure project
2. **Add MinIO config** to appsettings.json
3. **Register service** in Program.cs: `AddMinioObjectStorage()`
4. **Inject** `IObjectStorageService` into handlers
5. **Create API endpoints** following patterns in Section 6
6. **Implement CQRS handlers** following patterns in Section 7

### Essential Commands

```bash
# Create service account
docker exec shopnet-minio mc admin user add myminio [service-name] [password]

# Create bucket
docker exec shopnet-minio mc mb myminio/[bucket-name]

# List buckets
docker exec shopnet-minio mc ls myminio

# List files in bucket
docker exec shopnet-minio mc ls myminio/[bucket-name] --recursive

# Check service logs
docker logs shopnet-minio -f
```

### Configuration Template

```json
{
  "MinIO": {
    "Endpoint": "minio:9000",
    "AccessKey": "[service-name]-service",
    "SecretKey": "${[SERVICE]_MINIO_PASSWORD}",
    "BucketName": "[service]-files",
    "UseSSL": false,
    "Region": "us-east-1"
  }
}
```

---

## Related Documentation

- [Local Infrastructure Setup](./Local-Infrastructure-Setup.md) - Complete infrastructure guide
- [Design Patterns](./Design-Patterns.md) - Architectural patterns
- [CQRS Handler Registration Guide](./CQRS-Handler-Registration-Guide.md) - CQRS setup

---

**Document Version**: 1.0
**Last Updated**: 2025-10-30
**Maintained By**: Backend Team
**Related Services**: Catalog, Identity, Media (and all future services)
