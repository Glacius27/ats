namespace ats_service_discovery.Models;

public class ServiceEvent
{
    public string Event { get; set; } = default!;
    public string Service { get; set; } = default!;
    public string Host { get; set; } = default!;
    public int Port { get; set; }
}