using Ats.Integration.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ats.Integration;

public static class UserIntegrationExtensions
{
    public static IServiceCollection AddUserEvents(this IServiceCollection services)
    {
        return services;
    }

    public static void RegisterUserEventSubscriptions(this IServiceProvider sp)
    {
        var bus = sp.GetRequiredService<IAtsBus>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("UserEvents");

        var cache = sp.GetService<IUserCacheHandler>();
        if (cache is null)
        {
            logger.LogWarning("⚠️ IUserCacheHandler не зарегистрирован — обновления пользователей не будут применяться в кэше.");
        }

        bus.Subscribe<AuthUser>("user.created", async user =>
        {
            cache?.Upsert(user);
            logger.LogInformation("📩 user.created: {Username}", user.Username);
            await Task.CompletedTask;
        });

        bus.Subscribe<AuthUser>("user.updated", async user =>
        {
            cache?.Upsert(user);
            logger.LogInformation("📩 user.updated: {Username}", user.Username);
            await Task.CompletedTask;
        });

        bus.Subscribe<AuthUser>("user.deactivated", async user =>
        {
            cache?.Deactivate(user.Id);
            logger.LogInformation("📩 user.deactivated: {UserId}", user.Id);
            await Task.CompletedTask;
        });
    }
}