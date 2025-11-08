using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ats.Messaging.Abstractions;
using Ats.Messaging.Internal;
using Ats.Messaging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Ats.Messaging;

public sealed class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqConnection _connProvider;
    private readonly MessagingOptions _opt;
    private readonly ILogger<RabbitMqPublisher>? _logger;

    private IConnection? _conn;
    private IChannel? _channel;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RabbitMqPublisher(RabbitMqConnection connProvider, IOptions<MessagingOptions> opt, ILogger<RabbitMqPublisher>? logger = null)
    {
        _connProvider = connProvider;
        _opt = opt.Value;
        _logger = logger;
    }

    private async Task EnsureAsync()
    {
        _conn ??= await _connProvider.GetOrCreateConnectionAsync();
        _channel ??= await _conn.CreateChannelAsync();

        // Объявляем topic-exchange по умолчанию (идемпотентно)
        await _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
    }

    public async Task PublishAsync<TEvent>(TEvent evt, string? routingKey = null, string? exchange = null, string? correlationId = null, CancellationToken ct = default)
    {
        await EnsureAsync();

        var ex = exchange ?? _opt.Exchange;
        var rk = routingKey ?? typeof(TEvent).Name; // можно заменить на свой конвеншн

        var payload = JsonSerializer.SerializeToUtf8Bytes(evt!, JsonOpts);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = typeof(TEvent).Name,
            CorrelationId = correlationId
        };

        await _channel!.BasicPublishAsync(
            exchange: ex,
            routingKey: rk,
            mandatory: false,
            body: payload,
            basicProperties: props,
            cancellationToken: ct
        );

        _logger?.LogDebug("Published {Type} rk={RoutingKey} ex={Exchange} size={Size}B", typeof(TEvent).Name, rk, ex, payload.Length);
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_channel is not null) await _channel.CloseAsync(); } catch { }
        try { if (_conn is not null) await _conn.CloseAsync(); } catch { }
    }
}
