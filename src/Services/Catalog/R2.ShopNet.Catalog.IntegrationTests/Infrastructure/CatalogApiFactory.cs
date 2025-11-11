using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Persistence.Storage.MinIO;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace R2.ShopNet.Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing the Catalog API.
/// This factory sets up a test environment with PostgreSQL and MinIO test containers.
/// </summary>
public class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly MinioContainer _minioContainer;

    public CatalogApiFactory()
    {
        // Create a PostgreSQL test container
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("catalogdb_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        // Create a MinIO test container
        _minioContainer = new MinioBuilder()
            .WithImage("minio/minio:latest")
            .WithUsername("minioadmin")
            .WithPassword("minioadmin")
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Get MinIO endpoint from container (format: http://localhost:port)
            var minioUrl = new Uri(_minioContainer.GetConnectionString());
            var minioEndpoint = $"{minioUrl.Host}:{minioUrl.Port}";
            
            // Add in-memory configuration for MinIO
            var minioConfig = new Dictionary<string, string?>
            {
                ["MinIO:Endpoint"] = minioEndpoint,
                ["MinIO:AccessKey"] = "minioadmin",
                ["MinIO:SecretKey"] = "minioadmin",
                ["MinIO:BucketName"] = "test-bucket",
                ["MinIO:UseSSL"] = "false",
                ["MinIO:Region"] = "us-east-1"
            };
            config.AddInMemoryCollection(minioConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext configuration
            services.RemoveAll(typeof(DbContextOptions<CatalogDbContext>));
            services.RemoveAll<CatalogDbContext>();

            // Add DbContext with test database connection
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Disable authentication for testing
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultScheme = "TestScheme";
            });

            // Disable OpenIddict validation for testing
            services.RemoveAll<OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreOptions>();
            
            // Disable Consul service registration for testing
            services.RemoveAll<R2.ShopNet.Framework.ServiceDiscovery.ConsulServiceRegistration>();
        });

        // Set test environment
        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Initialize the database and MinIO containers before tests run
    /// </summary>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _minioContainer.StartAsync()
        );
    }

    /// <summary>
    /// Cleanup: Stop and dispose the containers after tests complete
    /// </summary>
    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StopAsync(),
            _minioContainer.StopAsync()
        );
        await Task.WhenAll(
            _dbContainer.DisposeAsync().AsTask(),
            _minioContainer.DisposeAsync().AsTask()
        );
    }

    /// <summary>
    /// Get the database connection string for direct database access in tests
    /// </summary>
    public string GetConnectionString() => _dbContainer.GetConnectionString();
}
