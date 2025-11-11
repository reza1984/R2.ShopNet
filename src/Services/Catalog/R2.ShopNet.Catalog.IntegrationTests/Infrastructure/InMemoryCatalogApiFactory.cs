using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using R2.ShopNet.Catalog.Infrastructure.Persistence;

namespace R2.ShopNet.Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Lightweight WebApplicationFactory using in-memory database for faster tests.
/// Use this for tests that don't need a real database (e.g., testing business logic).
/// Use CatalogApiFactory for full integration tests with PostgreSQL.
/// </summary>
public class InMemoryCatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext configuration
            services.RemoveAll(typeof(DbContextOptions<CatalogDbContext>));
            services.RemoveAll<CatalogDbContext>();

            // Add in-memory database
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseInMemoryDatabase($"CatalogTestDb_{Guid.NewGuid()}");
            });

            // Disable authentication for testing
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultScheme = "TestScheme";
            });

            // Disable OpenIddict validation for testing
            services.RemoveAll<OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreOptions>();

            // Build service provider and ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            // Ensure database is created
            dbContext.Database.EnsureCreated();
        });

        // Set test environment
        builder.UseEnvironment("Testing");
    }
}
