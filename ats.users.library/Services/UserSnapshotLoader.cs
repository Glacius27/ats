using System.Net.Http.Json;
using Ats.ServiceDiscovery.Client;          // ServiceDiscoveryClient, ServiceInstance
using Ats.Shared.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ats.Users.Services;

public class UserSnapshotLoader
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly UserCache _cache;
    private readonly ILogger<UserSnapshotLoader> _logger;
    private readonly ServiceDiscoveryClient _sd;   // конкретный класс SD-клиента

    public UserSnapshotLoader(
        HttpClient httpClient,
        IConfiguration config,
        UserCache cache,
        ILogger<UserSnapshotLoader> logger,
        ServiceDiscoveryClient sd)
    {
        _httpClient = httpClient;
        _config = config;
        _cache = cache;
        _logger = logger;
        _sd = sd;
    }

    public async Task LoadSnapshotAsync(CancellationToken ct = default)
    {
        try
        {
            string? baseUrl = null;

            // 1) Пробуем взять адрес Auth из Service Discovery (через кэш клиента)
            //    Делаем несколько попыток, т.к. кэш заполняется по таймеру RefreshCacheAsync().
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts && baseUrl == null; attempt++)
            {
                var instances = _sd.GetService("ats-authorization-service"); // List<ServiceInstance>? из кэша
                if (instances is { Count: > 0 })
                {
                    // возьмем первый попавшийся инстанс (при желании можно выбрать по Host == "localhost" и т.п.)
                    var svc = instances[0];
                    baseUrl = "http://" + svc.Host + ":" + svc.Port;
                    _logger.LogInformation("🔎 Auth URL via SD: {BaseUrl}", baseUrl);
                    break;
                }

                if (attempt < maxAttempts)
                {
                    _logger.LogWarning("⚠️ Attempt {Attempt}/{Max}: Auth not in SD cache yet, retrying...", attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
            }

            // 2) Фолбэк на конфиг (локалка/резерв)
            baseUrl ??= _config["AuthorizationService:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("❌ Auth base URL is not configured (no SD record and no AuthorizationService:BaseUrl).");
                return;
            }

            // 3) Запрос snapshot у ats-authorization-service
            var url = baseUrl.TrimEnd('/') + "/api/users/snapshot";
            _logger.LogInformation("📥 Loading user snapshot from {Url}", url);

            var users = await _httpClient.GetFromJsonAsync<List<AuthUser>>(url, ct) ?? new();
            _cache.ApplySnapshot(users);

            _logger.LogInformation("✅ Loaded {Count} users from snapshot", users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to load user snapshot");
        }
    }
}