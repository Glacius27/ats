using Ats.Integration.Contracts;


namespace CandidateService.Services;

public class UserCache
{
    private readonly Dictionary<Guid, AuthUser> _users = new();

    public IReadOnlyCollection<AuthUser> Users => _users.Values;

    public void Upsert(AuthUser user)
    {
        _users[user.Id] = user;
    }

    public void Deactivate(Guid id)
    {
        _users.Remove(id);
    }

    public void ApplySnapshot(IEnumerable<AuthUser> users)
    {
        _users.Clear();
        foreach (var u in users)
            _users[u.Id] = u;
    }
}