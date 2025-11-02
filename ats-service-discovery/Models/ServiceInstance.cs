namespace ServiceDiscovery.Models;

public class ServiceInstance
{
    public string Name { get; set; } = default!;
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string? HealthCheck { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}