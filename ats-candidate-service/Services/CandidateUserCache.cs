using Ats.Integration;
using Ats.Integration.Contracts;

namespace CandidateService.Services;

public class CandidateUserCache : IUserCacheHandler
{
    private readonly UserCache _cache;
    public CandidateUserCache(UserCache cache) => _cache = cache;

    public void Upsert(object user)
    {
        if (user is AuthUser u)
            _cache.Upsert(u);
    }

    public void Deactivate(Guid id) => _cache.Deactivate(id);

    public void ApplySnapshot(IEnumerable<object> users)
    {
        var typed = users.OfType<AuthUser>().ToList();
        _cache.ApplySnapshot(typed);
    }
}