using System.Collections.Concurrent;
using Ats.Integration;
using Ats.Integration.Contracts;

namespace Ats.CandidateService.Users;

public class UserCacheHandler : IUserCacheHandler
{
    private static readonly ConcurrentDictionary<Guid, AuthUser> _cache = new();

    public void ApplySnapshot(IEnumerable<AuthUser> users)
    {
        _cache.Clear();
        foreach (var u in users)
        {
            u.Roles ??= new List<string>();
            _cache[u.Id] = u;
        }
    }

    public void OnUserCreated(AuthUser user)
    {
        user.Roles ??= new List<string>();
        _cache[user.Id] = user;
    }

    public void OnUserUpdated(AuthUser user)
    {
        user.Roles ??= new List<string>();
        _cache[user.Id] = user;
    }

    public void OnUserDeactivated(AuthUser user)
    {
        if (_cache.TryGetValue(user.Id, out var existed))
        {
            existed.IsActive = false;
            _cache[user.Id] = existed;
        }
        else
        {
            user.Roles ??= new List<string>();
            user.IsActive = false;
            _cache[user.Id] = user;
        }
    }

    public bool TryGet(Guid id, out AuthUser? u) => _cache.TryGetValue(id, out u);
    public IReadOnlyCollection<AuthUser> All() => _cache.Values.ToList();
}