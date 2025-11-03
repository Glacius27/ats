using System;
using System.Threading.Tasks;
using Ats.Messaging.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Ats.Messaging.Internal;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly IOptions<MessagingOptions> _opt;
    private readonly ILogger<RabbitMqConnection>? _logger;

    private IConnection? _connection;

    public RabbitMqConnection(IOptions<MessagingOptions> opt, ILogger<RabbitMqConnection>? logger = null)
    {
        _opt = opt;
        _logger = logger;
    }

    public async Task<IConnection> GetOrCreateConnectionAsync()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        var o = _opt.Value;
        var factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true
        };

        _logger?.LogInformation("Connecting to RabbitMQ {User}@{Host}:{Port} vhost={VHost}", o.UserName, o.HostName, o.Port, o.VirtualHost);
        _connection = await factory.CreateConnectionAsync();
        _logger?.LogInformation("RabbitMQ connection established");

        return _connection!;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection is not null)
                await _connection.CloseAsync();
        }
        catch { /* ignore */ }
    }
}