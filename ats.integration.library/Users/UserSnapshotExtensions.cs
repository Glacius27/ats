using System.Net.Http.Json;
using Ats.Integration.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ats.Integration.Users;

public static class UserSnapshotExtensions
{
    public static IServiceCollection AddUserSnapshotHostedLoader<THandler>(this IServiceCollection services)
        where THandler : class, IUserCacheHandler
    {
        services.AddHttpClient<UserSnapshotLoader>();
        services.AddHostedService<UserSnapshotHostedService<THandler>>();
        return services;
    }

    private sealed class UserSnapshotHostedService<THandler> : IHostedService where THandler : class, IUserCacheHandler
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<UserSnapshotHostedService<THandler>> _logger;
        private readonly UserSnapshotLoader _loader;
        private readonly THandler _handler;

        public UserSnapshotHostedService(IServiceProvider sp, ILogger<UserSnapshotHostedService<THandler>> logger)
        {
            _sp = sp;
            _logger = logger;

            using var scope = _sp.CreateScope();
            _loader = scope.ServiceProvider.GetRequiredService<UserSnapshotLoader>();
            _handler = scope.ServiceProvider.GetRequiredService<THandler>();
        }

   
        public UserSnapshotHostedService(IServiceProvider sp) => _sp = sp;

        // public async Task StartAsync(CancellationToken cancellationToken)
        // {
        //     using var scope = _sp.CreateScope();
        //     var loader = scope.ServiceProvider.GetRequiredService<UserSnapshotLoader>();
        //     var handler = scope.ServiceProvider.GetRequiredService<THandler>();
        //
        //     var users = await loader.LoadSnapshotAsync(cancellationToken);
        //     handler.ApplySnapshot(users);
        // }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var attempts = 0;
                while (attempts < 2 && !cancellationToken.IsCancellationRequested)
                {
                    attempts++;
                    try
                    {
                        var users = await _loader.LoadSnapshotAsync(cancellationToken);
                        _handler.ApplySnapshot(users);
                        _logger.LogInformation("User snapshot applied: {Count} users", users.Count);
                        return;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "Snapshot attempt {Attempt} failed. Will retry shortly.", attempts);
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    }
                }

                _logger.LogWarning("User snapshot not loaded; starting without snapshot (will rely on events).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while loading user snapshot. Service will continue without it.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

internal sealed class UserSnapshotLoader
{
    private readonly HttpClient _http;
    private readonly ILogger<UserSnapshotLoader>? _log;
    private readonly string _baseUrl;

    public UserSnapshotLoader(HttpClient http, IConfiguration cfg, ILogger<UserSnapshotLoader>? log = null)
    {
        _http = http;
        _log = log;
        _baseUrl = cfg["AuthService:BaseUrl"] ?? cfg["AuthService__BaseUrl"] ?? "http://ats-authorization-service:8080";
    }

    public async Task<List<AuthUser>> LoadSnapshotAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl.TrimEnd('/')}/api/users/snapshot";
        _log?.LogInformation("Loading user snapshot from {Url}", url);
        return await _http.GetFromJsonAsync<List<AuthUser>>(url, ct) ?? new();
    }
}