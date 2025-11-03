using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;


namespace Ats.ServiceDiscovery.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection("ServiceDiscovery");
        services.Configure<ServiceDiscoveryOptions>(section);
        services.AddSingleton<ServiceDiscoveryClient>();
        services.AddHostedService<ServiceDiscoveryHostedService>(); 
        return services;
    }
}