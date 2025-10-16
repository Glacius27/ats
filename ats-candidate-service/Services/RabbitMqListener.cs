using System.Text;
using System.Text.Json;
using Ats.Shared.Auth;
using Ats.Shared.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CandidateService.Services;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "auth.events";
    public string Queue { get; set; } = "candidate-service-auth-events";
}

public class RabbitMqListener : BackgroundService
{
    private readonly UserCache _cache;
    private readonly ILogger<RabbitMqListener> _logger;
    private readonly RabbitMqOptions _opt;
    private IConnection? _conn;
    private IChannel? _channel;

    public RabbitMqListener(UserCache cache, IOptions<RabbitMqOptions> opt, ILogger<RabbitMqListener> logger)
    {
        _cache = cache;
        _logger = logger;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            UserName = _opt.UserName,
            Password = _opt.Password
        };

        _conn = await factory.CreateConnectionAsync();
        _channel = await _conn.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Topic, durable: true);
        await _channel.QueueDeclareAsync(_opt.Queue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(_opt.Queue, _opt.Exchange, "auth.user.*");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var baseEvent = JsonSerializer.Deserialize<EventBase>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                switch (baseEvent?.Type)
                {
                    case "auth.user.changed":
                        var changed = JsonSerializer.Deserialize<UserChangedEvent>(json);
                        if (changed?.User != null)
                            _cache.ApplyChange(changed.User);
                        break;

                    case "auth.user.deleted":
                        var deleted = JsonSerializer.Deserialize<UserDeletedEvent>(json);
                        if (deleted != null)
                            _cache.Remove(deleted.UserId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to process RabbitMQ message");
            }

            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(_opt.Queue, autoAck: true, consumer);
        _logger.LogInformation("🐇 Listening for user events from RabbitMQ...");

        await Task.Delay(-1, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_conn != null) await _conn.CloseAsync();
        _logger.LogInformation("🛑 RabbitMQ listener stopped.");
    }
}