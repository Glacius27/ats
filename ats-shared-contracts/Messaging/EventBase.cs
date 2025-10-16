namespace Ats.Shared.Messaging;
public abstract record EventBase
{
    public abstract string Type { get; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}