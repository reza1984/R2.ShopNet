using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class ControllersExtension
    {
        public static IServiceCollection AddCatalogControllers(this IServiceCollection services)
        {
            services.AddOpenApi();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            return services;
        }
    }
}
