using Ats.Integration.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ats.Integration.Users;

public static class UserIntegrationExtensions
{
    public static IServiceCollection AddUserEvents(this IServiceCollection services)
    {
        services.AddHostedService<UserEventsHostedService>();
        return services;
    }

    private sealed class UserEventsHostedService : IHostedService
    {
        private readonly IAtsBus _bus;
        private readonly ILogger<UserEventsHostedService> _log;
        private readonly IServiceProvider _sp;

        public UserEventsHostedService(IAtsBus bus, ILogger<UserEventsHostedService> log, IServiceProvider sp)
        {
            _bus = bus;
            _log = log;
            _sp = sp;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bus.SubscribeAsync<AuthUser>("user.created", async u =>
            {
                using var scope = _sp.CreateScope();
                var handler = scope.ServiceProvider.GetService<IUserCacheHandler>();
                handler?.OnUserCreated(u);
                _log.LogInformation("User created: {User}", u.Username);
                await Task.CompletedTask;
            });

            await _bus.SubscribeAsync<AuthUser>("user.updated", async u =>
            {
                using var scope = _sp.CreateScope();
                var handler = scope.ServiceProvider.GetService<IUserCacheHandler>();
                handler?.OnUserUpdated(u);
                _log.LogInformation("User updated: {User}", u.Username);
                await Task.CompletedTask;
            });

            await _bus.SubscribeAsync<AuthUser>("user.deactivated", async u =>
            {
                using var scope = _sp.CreateScope();
                var handler = scope.ServiceProvider.GetService<IUserCacheHandler>();
                handler?.OnUserDeactivated(u);
                _log.LogInformation("User deactivated: {User}", u.Username);
                await Task.CompletedTask;
            });

            _log.LogInformation("User event subscriptions established");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}