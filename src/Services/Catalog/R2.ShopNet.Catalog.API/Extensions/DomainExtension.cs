using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Framework.CQRS.Generated;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class DomainExtension
    {
        public static IServiceCollection AddCatalogDomain(this IServiceCollection services)
        {
            services.AddGeneratedCQRSHandlers();
            return services;
        }
    }
}
