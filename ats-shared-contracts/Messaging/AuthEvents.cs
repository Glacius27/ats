using Ats.Shared.Auth;

namespace Ats.Shared.Messaging;

public record UserChangedEvent(AuthUser User) : EventBase
{
    public override string Type => "auth.user.changed";
}

public record UserDeletedEvent(Guid UserId) : EventBase
{
    public override string Type => "auth.user.deleted";
}