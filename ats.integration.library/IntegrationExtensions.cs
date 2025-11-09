using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Ats.Integration;

public static class IntegrationExtensions
{
    public static IServiceCollection AddAtsIntegration(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<IntegrationOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<IAtsBus, AtsBus>();
        return services;
    }

    public static async Task UseAtsIntegrationAsync(this IApplicationBuilder app)
    {
        var bus = app.ApplicationServices.GetRequiredService<IAtsBus>();
        app.ApplicationServices.RegisterUserEventSubscriptions();
        await Task.CompletedTask; // placeholder for future auto-registration
    }
}