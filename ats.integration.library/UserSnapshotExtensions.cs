using Ats.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;


public static class UserSnapshotExtensions
{
    public static IServiceCollection AddUserSnapshot(this IServiceCollection services)
    {
        services.AddHttpClient<UserSnapshotLoader>();
        services.AddSingleton<UserSnapshotLoader>();
        return services;
    }
}