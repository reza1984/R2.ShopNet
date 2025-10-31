using Microsoft.Extensions.DependencyInjection;
using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Catalog.API.Extensions
{
    public static class EventingExtension
    {
        public static IServiceCollection AddCatalogEventing(this IServiceCollection services)
        {
            services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
            return services;
        }
    }
}
