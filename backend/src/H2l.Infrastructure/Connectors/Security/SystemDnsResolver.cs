using System.Net;

namespace H2l.Infrastructure.Connectors.Security;

public sealed class SystemDnsResolver : IDnsResolver
{
    public Task<IPAddress[]> ResolveHostAddressesAsync(string host, CancellationToken cancellationToken = default)
        => Dns.GetHostAddressesAsync(host, cancellationToken);
}
