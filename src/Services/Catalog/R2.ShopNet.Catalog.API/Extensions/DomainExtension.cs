using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Catalog.Application.Commands;
using R2.ShopNet.Framework.CQRS.DependencyInjection;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class DomainExtension
    {
        public static IServiceCollection AddCatalogDomain(this IServiceCollection services)
        {
            services.AddCQRSHandlersFromAssemblyContaining<CreateProductCommandHandler>();
            return services;
        }
    }
}
