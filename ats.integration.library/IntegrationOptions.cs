namespace Ats.Integration;

public class IntegrationOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "ats_user";
    public string Password { get; set; } = "ats_pass";
    public string Exchange { get; set; } = "auth.events";
    public string ClientId { get; set; } = "default";
    public ushort PrefetchCount { get; set; } = 10;
}