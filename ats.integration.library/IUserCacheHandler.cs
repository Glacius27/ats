namespace Ats.Integration;

public interface IUserCacheHandler
{
    void Upsert(object user);
    void Deactivate(Guid id);
}