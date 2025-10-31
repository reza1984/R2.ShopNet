using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class HealthChecksExtension
    {
        public static IServiceCollection AddCatalogHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks();
            return services;
        }
    }
}
