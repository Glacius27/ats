using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Ats.ServiceDiscovery.Client;

public class ServiceDiscoveryClient : IDisposable, IServiceDiscoveryClient
{
    private readonly HttpClient _http;
    private readonly ServiceDiscoveryOptions _opt;
    private readonly ILogger<ServiceDiscoveryClient> _log;
    private readonly Timer _heartbeatTimer;
    private readonly Timer _refreshTimer;
    private Dictionary<string, List<ServiceInstance>> _cache = new();

    public ServiceDiscoveryClient(IOptions<ServiceDiscoveryOptions> options, ILogger<ServiceDiscoveryClient> logger)
    {
        _opt = options.Value;
        _log = logger;
        _http = new HttpClient { BaseAddress = new Uri(_opt.ServiceDiscoveryUrl) };

  
        _heartbeatTimer = new Timer(async _ => await RegisterAsync(), null, TimeSpan.Zero, 
            TimeSpan.FromSeconds(_opt.HeartbeatIntervalSeconds));

        _refreshTimer = new Timer(async _ => await RefreshCacheAsync(), null, TimeSpan.Zero, 
            TimeSpan.FromSeconds(_opt.RefreshIntervalSeconds));
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(_opt.ServiceName))
        {
            _log.LogWarning("Skipping registration — ServiceName not yet set");
            return;
        }
        
        try
        {
            var instance = new
            {
                name = _opt.ServiceName,
                host = _opt.Host,
                port = _opt.Port
            };

            var resp = await _http.PostAsJsonAsync("/register", instance);
            if (resp.IsSuccessStatusCode)
                _log.LogInformation("Service {Service} registered successfully", _opt.ServiceName);
            else
                _log.LogWarning("Failed to register {Service}: {Code}", _opt.ServiceName, resp.StatusCode);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error registering service {Service}", _opt.ServiceName);
        }
    }

    private async Task RefreshCacheAsync()
    {
        try
        {
            var services = await _http.GetFromJsonAsync<List<ServiceInstance>>("/services");
            if (services != null)
            {
                _cache = services
                    .GroupBy(s => s.Name)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Error refreshing service list");
        }
    }

    public List<ServiceInstance>? GetService(string name)
    {
        _cache.TryGetValue(name, out var instances);
        return instances;
    }

    public void Dispose()
    {
        _heartbeatTimer.Dispose();
        _refreshTimer.Dispose();
        _http.Dispose();
    }
    
    public void SetServiceMetadata(string name, string host, int port)
    {
        _opt.ServiceName = name;
        _opt.Host = host;
        _opt.Port = port;
    }

    public void StartBackgroundWork()
    {
        _ = RegisterAsync();
        _heartbeatTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_opt.HeartbeatIntervalSeconds));
        _refreshTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_opt.RefreshIntervalSeconds));
    }
    public async Task<IReadOnlyList<ServiceInstance>> GetServiceAsync(string name, CancellationToken ct = default)
    {
        
        if (_cache.Count == 0)
        {
            await RefreshCacheAsync();
        }

        _cache.TryGetValue(name, out var list);
        return list ?? new List<ServiceInstance>();
    }

    public async Task<ServiceInstance?> GetSingleServiceAsync(string name, CancellationToken ct = default)
    {
        var list = await GetServiceAsync(name, ct);
        return list.FirstOrDefault();
    }

}

public record ServiceInstance(string Name, string Host, int Port);
