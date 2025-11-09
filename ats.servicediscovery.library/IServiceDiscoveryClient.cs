namespace Ats.ServiceDiscovery.Client;

public interface IServiceDiscoveryClient
{
    Task<IReadOnlyList<ServiceInstance>> GetServiceAsync(string name, CancellationToken ct = default);

    Task<ServiceInstance?> GetSingleServiceAsync(string name, CancellationToken ct = default);
}