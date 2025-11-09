using System.Net.Http.Json;
using Ats.Integration.Contracts;
using Ats.ServiceDiscovery.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
namespace Ats.Integration;

public class UserSnapshotLoader
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<UserSnapshotLoader> _logger;
    private readonly IServiceProvider _sp;

    public UserSnapshotLoader(HttpClient httpClient, IConfiguration config, ILogger<UserSnapshotLoader> logger, IServiceProvider sp)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _sp = sp;
    }

    public async Task<List<AuthUser>> LoadSnapshotAsync(CancellationToken ct = default)
    {
        try
        {
            string? baseUrl = null;

            
            try
            {
                var sdClient = _sp.GetService<ServiceDiscoveryClient>();
                if (sdClient != null)
                {
                    var instances = sdClient.GetService("ats-authorization-service");
                    var svc = instances?.FirstOrDefault();
                    if (svc != null)
                    {
                        baseUrl = $"http://{svc.Host}:{svc.Port}";
                        _logger.LogInformation("🔎 Resolved ats-auth via SD: {BaseUrl}", baseUrl);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Auth service not found in SD cache.");
                    }
                }
                else
                {
                    _logger.LogDebug("ℹ️ Service Discovery client not registered, fallback to config.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to query Service Discovery, fallback to config.");
            }

            
            baseUrl ??= _config["AuthorizationService:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("❌ Auth base URL is not configured (no SD and no config).");
                return new List<AuthUser>();
            }

          
            var url = $"{baseUrl.TrimEnd('/')}/api/users/snapshot";
            _logger.LogInformation("📥 Fetching user snapshot from {Url}", url);

            var users = await _httpClient.GetFromJsonAsync<List<AuthUser>>(url, ct) ?? new();
            _logger.LogInformation("✅ Loaded {Count} users from snapshot", users.Count);

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to load user snapshot");
            return new List<AuthUser>();
        }
    }
}