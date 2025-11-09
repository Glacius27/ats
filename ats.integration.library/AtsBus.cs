using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ats.Integration;

public class AtsBus : IAtsBus
{
    private readonly IntegrationOptions _opt;
    private readonly ILogger<AtsBus> _logger;
    private readonly IConnection _conn;
    private readonly IChannel _channel;

    public AtsBus(IOptions<IntegrationOptions> opt, ILogger<AtsBus> logger)
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
        
        _conn = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _conn.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
        _logger.LogInformation("🐇 Connected to RabbitMQ at {Host}:{Port}", _opt.HostName, _opt.Port);
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(_opt.Exchange, routingKey, body);
        _logger.LogInformation("📤 Published {RoutingKey}: {Json}", routingKey, json);
    }

    public void Subscribe<T>(string routingKey, Func<T, Task> handler)
    {
        var queueName = $"{_opt.ClientId}.{routingKey.Replace('.', '-')}-queue";

        _ = Task.Run(async () =>
        {
            await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(queueName, _opt.Exchange, routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize<T>(json);
                if (obj is not null)
                    await handler(obj);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer);
            _logger.LogInformation("📥 Subscribed: {RoutingKey} → {Queue}", routingKey, queueName);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_conn is not null)
            await _conn.CloseAsync();
    }
}