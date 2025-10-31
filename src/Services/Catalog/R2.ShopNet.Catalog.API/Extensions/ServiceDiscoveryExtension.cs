using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using R2.ShopNet.Framework.ServiceDiscovery;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class ServiceDiscoveryExtension
    {
        public static IServiceCollection AddCatalogServiceDiscovery(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddConsulServiceDiscovery(configuration);
            return services;
        }
    }
}
