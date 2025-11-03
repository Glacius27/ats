namespace Ats.ServiceDiscovery.Client;

public class ServiceDiscoveryOptions
{
    public string ServiceDiscoveryUrl { get; set; } = default!;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int RefreshIntervalSeconds { get; set; } = 15;
    
    public bool UseLocalhostAsHost { get; set; } = false; 

    // Эти поля теперь будут задаваться динамически
    public string ServiceName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}
