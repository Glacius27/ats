using System.Collections.Concurrent;
using Ats.Shared.Auth;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CandidateService.Services;

public class UserCache
{
    private readonly ConcurrentDictionary<Guid, AuthUser> _users = new();
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserCache> _logger;
    private readonly IConfiguration _config;

    public UserCache(HttpClient httpClient, ILogger<UserCache> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public IReadOnlyDictionary<Guid, AuthUser> Users => _users;

    public async Task LoadSnapshotAsync(CancellationToken ct = default)
    {
        try
        {
            var baseUrl = _config["AuthorizationService:BaseUrl"] ?? throw new InvalidOperationException("AuthorizationService:BaseUrl not configured");
            _logger.LogInformation("📥 Loading user snapshot from {Url}", baseUrl);

            var users = await _httpClient.GetFromJsonAsync<List<AuthUser>>($"{baseUrl}/api/users/snapshot", ct);

            if (users == null)
            {
                _logger.LogWarning("⚠️ Snapshot returned null.");
                return;
            }

            _users.Clear();
            foreach (var user in users)
                _users[user.Id] = user;

            _logger.LogInformation("✅ Loaded {Count} users from snapshot.", _users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to load user snapshot.");
        }
    }

    public void ApplyChange(AuthUser user)
    {
        _users[user.Id] = user;
        _logger.LogInformation("🔄 User updated: {Username}", user.Username);
    }

    public void Remove(Guid id)
    {
        if (_users.TryRemove(id, out var removed))
            _logger.LogInformation("🗑 User removed: {Username}", removed.Username);
    }
}