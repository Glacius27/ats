using Ats.Users.Messaging;
using Ats.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ats.Users.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserIntegration(this IServiceCollection services)
    {
        services.AddSingleton<UserCache>();
        services.AddHttpClient<UserSnapshotLoader>();
        services.AddSingleton<UserEventsSubscriber>();
        return services;
    }
}