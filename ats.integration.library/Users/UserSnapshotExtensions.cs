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
        public UserSnapshotHostedService(IServiceProvider sp) => _sp = sp;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _sp.CreateScope();
            var loader = scope.ServiceProvider.GetRequiredService<UserSnapshotLoader>();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();

            var users = await loader.LoadSnapshotAsync(cancellationToken);
            handler.ApplySnapshot(users);
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
        _baseUrl = cfg["AuthService:BaseUrl"] ?? "http://ats-authorization-service";
    }

    public async Task<List<AuthUser>> LoadSnapshotAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl.TrimEnd('/')}/api/users/snapshot";
        _log?.LogInformation("Loading user snapshot from {Url}", url);
        return await _http.GetFromJsonAsync<List<AuthUser>>(url, ct) ?? new();
    }
}