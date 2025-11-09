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
            _cache[u.Id] = u;
    }

    //public void OnUserCreated(AuthUser user) => _cache[user.Id] = user;
    public void OnUserCreated(AuthUser user)
    {
        _cache[user.Id] = user;
        Console.WriteLine($"[CACHE] Added user: {user.Username} ({user.Email})");
    }

    public void OnUserUpdated(AuthUser user) => _cache[user.Id] = user;

    public void OnUserDeactivated(AuthUser user)
    {
        if (_cache.TryGetValue(user.Id, out var existed))
        {
            existed.IsActive = false;
            _cache[user.Id] = existed;
        }
        else
        {
            _cache[user.Id] = user;
        }
    }

    public bool TryGet(Guid id, out AuthUser? user) => _cache.TryGetValue(id, out user);

    public IReadOnlyCollection<AuthUser> All() => _cache.Values.ToList();
}