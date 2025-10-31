using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class PersistenceExtension
    {
        public static IServiceCollection AddCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CatalogDb")
                ?? "Host=localhost;Port=5432;Database=r2shopnet_catalog;Username=postgres;Password=postgres";
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseNpgsql(connectionString));
            services.AddScoped<IUnitOfWork>(sp =>
                new UnitOfWork(sp.GetRequiredService<CatalogDbContext>()));
            return services;
        }
    }
}
