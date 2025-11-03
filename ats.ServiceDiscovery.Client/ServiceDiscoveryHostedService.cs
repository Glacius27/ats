using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Net;
using Microsoft.Extensions.Options;


namespace Ats.ServiceDiscovery.Client;

public class ServiceDiscoveryHostedService : IHostedService
{
    private readonly ServiceDiscoveryClient _client;
    private readonly ILogger<ServiceDiscoveryHostedService> _log;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServer _server;
    private readonly ServiceDiscoveryOptions _opt;

    public ServiceDiscoveryHostedService(
        ServiceDiscoveryClient client,
        ILogger<ServiceDiscoveryHostedService> log,
        IHostApplicationLifetime lifetime,
        IServer server,
        IOptions<ServiceDiscoveryOptions> opt)
    {
        _client = client;
        _log = log;
        _lifetime = lifetime;
        _server = server;
        _opt = opt.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
                var port = 0;
                string? address = null;

                if (addresses != null && addresses.Any())
                {
                    // Берём первый адрес, например "http://localhost:5002"
                    var uri = new Uri(addresses.First());
                    port = uri.Port;
                    address = uri.Host;
                }

                var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-service";
                
                string host;

                if (_opt.UseLocalhostAsHost)
                {
                    host = "localhost";
                }
                else
                {
                    host = Dns.GetHostName();
                }

                _log.LogInformation("🟢 Registering service automatically: {Service} ({Host}:{Port})", serviceName, host, port);

                _client.SetServiceMetadata(serviceName, host, port);
                _client.StartBackgroundWork(); // включает heartbeat и refresh
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to auto-register service in Service Discovery");
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("🔴 Service Discovery stopped");
        _client.Dispose();
        return Task.CompletedTask;
    }
}
