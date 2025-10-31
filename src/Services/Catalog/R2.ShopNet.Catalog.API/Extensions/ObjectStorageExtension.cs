using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using R2.ShopNet.Framework.Persistence.Storage.Extensions;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Infrastructure.Repositories;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class ObjectStorageExtension
    {
        public static IServiceCollection AddCatalogObjectStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMinioObjectStorage(configuration);
            services.AddScoped<IMinIORepository<Product>, ProductImageRepository>();
            return services;
        }
    }
}
