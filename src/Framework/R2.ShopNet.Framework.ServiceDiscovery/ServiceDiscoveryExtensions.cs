using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace R2.ShopNet.Framework.ServiceDiscovery;

/// <summary>
/// Extension methods for registering service discovery.
/// </summary>
public static class ServiceDiscoveryExtensions
{
    public static IServiceCollection AddConsulServiceDiscovery(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "Consul")
    {
        services.Configure<ConsulOptions>(configuration.GetSection(configSectionName));

        services.AddSingleton<IConsulClient, ConsulClient>(p =>
        {
            var consulOptions = configuration.GetSection(configSectionName).Get<ConsulOptions>();
            return new ConsulClient(config =>
            {
                config.Address = new Uri(consulOptions?.Address ?? "http://localhost:8500");
            });
        });

        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
        services.AddHostedService<ConsulServiceRegistration>();

        return services;
    }
}
