using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class CorsExtension
    {
        public static IServiceCollection AddCatalogCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            return services;
        }
    }
}
