using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using Respawn;

namespace R2.ShopNet.Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests providing common setup and teardown functionality.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CatalogApiFactory>, IAsyncLifetime
{
    protected readonly CatalogApiFactory Factory;
    protected readonly HttpClient Client;
    private Respawner _respawner = default!;

    protected IntegrationTestBase(CatalogApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Initialize test setup (runs once before all tests in the class)
    /// </summary>
    public async Task InitializeAsync()
    {
        // Ensure database is created and migrations are applied
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        
        // Initialize Respawner for database cleanup between tests
        await using var connection = new NpgsqlConnection(Factory.GetConnectionString());
        await connection.OpenAsync();
        
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "catalog" },
            TablesToIgnore = new[]
            {
                new Respawn.Graph.Table("catalog", "__EFMigrationsHistory")
            }
        });
    }

    /// <summary>
    /// Cleanup after all tests in the class complete
    /// </summary>
    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reset the database to a clean state before each test.
    /// Call this method at the beginning of each test method.
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(Factory.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Get a scoped service from the test server's service provider
    /// </summary>
    protected async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Execute an action with a scoped service provider
    /// </summary>
    protected async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Get the database context for direct database operations in tests
    /// </summary>
    protected async Task<TResult> WithDbContextAsync<TResult>(Func<CatalogDbContext, Task<TResult>> action)
    {
        return await ExecuteInScopeAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<CatalogDbContext>();
            return await action(dbContext);
        });
    }

    /// <summary>
    /// Execute an action with database context
    /// </summary>
    protected async Task WithDbContextAsync(Func<CatalogDbContext, Task> action)
    {
        await ExecuteInScopeAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<CatalogDbContext>();
            await action(dbContext);
        });
    }
}
