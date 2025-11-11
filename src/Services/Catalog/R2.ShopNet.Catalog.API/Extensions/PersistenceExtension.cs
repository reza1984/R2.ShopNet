using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Persistence.Extensions;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class PersistenceExtension
    {
        public static IServiceCollection AddCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CatalogDb");
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Register HttpContextAccessor for current user access
            services.AddHttpContextAccessor();

            // Register current user accessor for audit tracking
            services.AddCurrentUserAccessor();

            // Register UnitOfWork and repositories using framework extensions
            services.AddPersistence<CatalogDbContext>();

            return services;
        }
    }
}
