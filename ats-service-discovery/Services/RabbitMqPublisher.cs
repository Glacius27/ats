using System.Text;
using System.Text.Json;
using ats_service_discovery.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ServiceDiscovery.Models; // поменяй namespace, если нужно

namespace ServiceDiscovery.Services;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "service-discovery";
}

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly RabbitMqOptions _opt;
    private readonly IConnection _conn;
    private readonly IChannel _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> opt)
    {
        _opt = opt.Value;

        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            UserName = _opt.UserName,
            Password = _opt.Password
        };

        // Создаем асинхронное соединение и канал
        _conn = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _conn.CreateChannelAsync().GetAwaiter().GetResult();

        // Объявляем fanout exchange для рассылки событий
        _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Fanout, durable: true)
            .GetAwaiter().GetResult();
    }

    public async Task PublishAsync(ServiceEvent evt, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(
            exchange: _opt.Exchange,
            routingKey: "",
            mandatory: false,
            body: body,
            basicProperties: props,
            cancellationToken: ct
        );
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _conn.CloseAsync();
    }
}
