using Ats.Users.Services;
using Ats.Messaging.Abstractions;
using Ats.Shared.Auth;
using Microsoft.Extensions.Logging;

namespace Ats.Users.Messaging;

public class UserEventsSubscriber : IEventSubscriber
{
    private readonly UserCache _cache;
    private readonly ILogger<UserEventsSubscriber> _logger;

    public UserEventsSubscriber(UserCache cache, ILogger<UserEventsSubscriber> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    [Topic("user.created")]
    public Task OnUserCreated(AuthUser user)
    {
        _cache.Upsert(user);
        _logger.LogInformation("📩 Received user.created for {Username}", user.Username);
        return Task.CompletedTask;
    }

    [Topic("user.updated")]
    public Task OnUserUpdated(AuthUser user)
    {
        _cache.Upsert(user);
        _logger.LogInformation("📩 Received user.updated for {Username}", user.Username);
        return Task.CompletedTask;
    }

    [Topic("user.deactivated")]
    public Task OnUserDeactivated(AuthUser user)
    {
        _cache.Deactivate(user.Id);
        _logger.LogInformation("📩 Received user.deactivated for {UserId}", user.Id);
        return Task.CompletedTask;
    }

}