using System;
using System.Threading.Tasks;

namespace Ats.Messaging.Abstractions;

public interface IEventSubscriber : IAsyncDisposable
{
    void Subscribe<TEvent>(string queueName, Func<TEvent, Task> handler, string? routingKey = null, string? exchange = null);
}