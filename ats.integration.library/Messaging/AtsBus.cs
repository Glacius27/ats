using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ats.Integration.Messaging;

public sealed class AtsBus : IAtsBus, IAsyncDisposable
{
    private readonly IntegrationOptions _opt;
    private readonly ILogger<AtsBus>? _logger;
    private readonly IConnection _conn;
    private IChannel? _channel;

    public AtsBus(IOptions<IntegrationOptions> opt, ILogger<AtsBus>? logger = null)
    {
        _opt = opt.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            UserName = _opt.UserName,
            Password = _opt.Password
        };

        // Новая сигнатура: асинхронное создание соединения
        _conn = factory.CreateConnectionAsync(_opt.ClientId).GetAwaiter().GetResult();
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: true }) return _channel;

        _channel = await _conn.CreateChannelAsync();
        await _channel.ExchangeDeclareAsync(
            exchange: _opt.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        return _channel;
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
    {
        var ch = await GetChannelAsync();
        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await ch.BasicPublishAsync(
            exchange: _opt.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger?.LogInformation("Published {Type} to {Exchange}:{RoutingKey}",
            typeof(T).Name, _opt.Exchange, routingKey);
    }

    public async Task SubscribeAsync<T>(string routingKey, Func<T, Task> handler, CancellationToken ct = default)
    {
        var ch = await GetChannelAsync();

        var queue = $"q.{_opt.ClientId}.{routingKey}"
            .Replace('*', '_')
            .Replace('#', '_')
            .ToLowerInvariant();

        await ch.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
        await ch.QueueBindAsync(queue, _opt.Exchange, routingKey);

        var consumer = new AsyncEventingBasicConsumer(ch);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var payload = JsonSerializer.Deserialize<T>(json)!;
                await handler(payload);
                await ch.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while handling message {RoutingKey}", ea.RoutingKey);
                await ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await ch.BasicQosAsync(prefetchSize: 0, prefetchCount: 16, global: false);
        await ch.BasicConsumeAsync(queue, autoAck: false, consumer);

        _logger?.LogInformation("Subscribed to {Exchange}:{RoutingKey} as queue {Queue}", _opt.Exchange, routingKey, queue);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is not null)
            {
                try { await _channel.CloseAsync(); } catch { }
                _channel.Dispose();
            }
        }
        catch { /* ignore */ }

        try { await _conn.CloseAsync(); } catch { }
        _conn.Dispose();
    }
}