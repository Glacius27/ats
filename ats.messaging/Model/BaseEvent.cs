using System;

namespace Ats.Messaging.Model;

public abstract class BaseEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}