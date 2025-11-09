using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;


namespace Ats.ServiceDiscovery.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ServiceDiscoveryOptions>(config.GetSection("ServiceDiscovery"));
        services.AddSingleton<ServiceDiscoveryClient>();
        services.AddSingleton<IServiceDiscoveryClient>(sp => sp.GetRequiredService<ServiceDiscoveryClient>());
        services.AddHostedService<ServiceDiscoveryHostedService>();
        return services;
    }
}