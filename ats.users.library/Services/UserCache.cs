using System.Collections.Concurrent;
using Ats.Shared.Auth;
using Microsoft.Extensions.Logging;

namespace Ats.Users.Services;

public class UserCache
{
    private readonly ConcurrentDictionary<Guid, AuthUser> _users = new();
    private readonly ILogger<UserCache> _logger;

    public UserCache(ILogger<UserCache> logger) => _logger = logger;

    public IReadOnlyDictionary<Guid, AuthUser> Users => _users;

    public void ApplySnapshot(IEnumerable<AuthUser> users)
    {
        _users.Clear();
        foreach (var u in users)
            _users[u.Id] = u;
        _logger.LogInformation("✅ User snapshot loaded ({Count})", _users.Count);
    }

    public void Upsert(AuthUser user)
    {
        _users[user.Id] = user;
        _logger.LogInformation("🔄 User upserted: {Username}", user.Username);
    }

    public void Deactivate(Guid id)
    {
        if (_users.TryGetValue(id, out var user))
        {
            user.IsActive = false;
            _logger.LogInformation("🚫 User deactivated: {Username}", user.Username);
        }
    }
}