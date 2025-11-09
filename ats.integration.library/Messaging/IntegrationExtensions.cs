using Ats.Integration.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ats.Integration.Messaging;

public static class IntegrationExtensions
{
    public static IServiceCollection AddAtsIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<IAtsBus, AtsBus>();
        return services;
    }
}