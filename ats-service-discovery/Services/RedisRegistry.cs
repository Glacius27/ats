using ServiceDiscovery.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace ServiceDiscovery.Services;

public class RedisRegistry
{
    private readonly IDatabase _db;
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(60);

    public RedisRegistry(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<bool> RegisterAsync(ServiceInstance instance)
    {
        // Валидируем имя
        if (string.IsNullOrWhiteSpace(instance.Name))
            throw new ArgumentException("Service name cannot be empty");

        var key = $"service:{instance.Name}:{instance.Host}:{instance.Port}";
        var value = JsonSerializer.Serialize(instance);

        var exists = await _db.KeyExistsAsync(key);

        // Обновляем или создаём запись с TTL
        await _db.StringSetAsync(key, value, _ttl);

        // Вернём true, если запись новая
        return !exists;
    }

    public async Task<IEnumerable<ServiceInstance>> GetAllAsync()
    {
        var server = GetServer();
        var keys = server.Keys(pattern: "service:*");
        var results = new List<ServiceInstance>();

        foreach (var key in keys)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.IsNullOrEmpty)
            {
                var instance = JsonSerializer.Deserialize<ServiceInstance>(value!);
                if (instance != null)
                    results.Add(instance);
            }
        }

        return results;
    }

    public async Task<IEnumerable<ServiceInstance>> GetByNameAsync(string name)
    {
        var server = GetServer();
        var keys = server.Keys(pattern: $"service:{name}:*");
        var results = new List<ServiceInstance>();

        foreach (var key in keys)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.IsNullOrEmpty)
            {
                var instance = JsonSerializer.Deserialize<ServiceInstance>(value!);
                if (instance != null)
                    results.Add(instance);
            }
        }

        return results;
    }

    private IServer GetServer()
    {
        var endpoints = _db.Multiplexer.GetEndPoints();
        return _db.Multiplexer.GetServer(endpoints.First());
    }
}
