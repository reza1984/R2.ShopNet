using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.Storage.MinIO;

namespace R2.ShopNet.Framework.Persistence.Storage.Extensions;

public static class StorageServiceCollectionExtensions
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
