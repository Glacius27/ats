namespace Ats.Integration;

public interface IAtsBus : IAsyncDisposable
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
    void Subscribe<T>(string routingKey, Func<T, Task> handler);
}