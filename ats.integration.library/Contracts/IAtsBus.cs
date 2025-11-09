using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ats.Integration;

public interface IAtsBus : IAsyncDisposable
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default);
    Task SubscribeAsync<T>(string routingKey, Func<T, Task> handler, CancellationToken ct = default);
}