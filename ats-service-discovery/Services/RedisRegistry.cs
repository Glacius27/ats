using ServiceDiscovery.Models;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using IServer = StackExchange.Redis.IServer;

namespace ServiceDiscovery.Services;

public class RedisRegistry
{
    private readonly IDatabase _db;
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(60); // время жизни регистрации

    public RedisRegistry(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task RegisterAsync(ServiceInstance instance)
    {
        var key = $"service:{instance.Name}:{Guid.NewGuid()}";
        var value = JsonSerializer.Serialize(instance);
        await _db.StringSetAsync(key, value, _ttl);
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
        // используется для поиска ключей
        var endpoints = _db.Multiplexer.GetEndPoints();
        return _db.Multiplexer.GetServer(endpoints.First());
    }
}
