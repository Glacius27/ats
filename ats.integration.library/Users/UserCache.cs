using System.Collections.Concurrent;
using Ats.Integration.Contracts;

namespace Ats.CandidateService.Users;

public class UserCache
{
    private readonly ConcurrentDictionary<Guid, AuthUser> _cache = new();


    public IReadOnlyCollection<AuthUser> All => _cache.Values.ToList();

 
    public void Set(AuthUser user)
    {
        _cache[user.Id] = user;
    }

    public void MarkDeactivated(Guid id)
    {
        if (_cache.TryGetValue(id, out var u))
        {
            u.IsActive = false;
            _cache[id] = u;
        }
        else
        {
            
            _cache[id] = new AuthUser
            {
                Id = id,
                IsActive = false,
                Username = "",
                Email = "",
                Roles = new List<string>()
            };
        }
    }

    public bool TryGet(Guid id, out AuthUser? user)
    {
        return _cache.TryGetValue(id, out user);
    }
}