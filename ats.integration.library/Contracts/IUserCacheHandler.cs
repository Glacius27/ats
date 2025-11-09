using Ats.Integration.Contracts;

namespace Ats.Integration;

public interface IUserCacheHandler
{
    // Снапшот разом
    void ApplySnapshot(IEnumerable<AuthUser> users);

    // Инкрементальные события
    void OnUserCreated(AuthUser user);
    void OnUserUpdated(AuthUser user);
    void OnUserDeactivated(AuthUser user);
}