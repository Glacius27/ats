using System.Threading;
using System.Threading.Tasks;

namespace Ats.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent evt, string? routingKey = null, string? exchange = null, string? correlationId = null, CancellationToken ct = default);
}
