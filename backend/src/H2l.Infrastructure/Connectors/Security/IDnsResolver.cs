using System.Net;

namespace H2l.Infrastructure.Connectors.Security;

public interface IDnsResolver
{
    Task<IPAddress[]> ResolveHostAddressesAsync(string host, CancellationToken cancellationToken = default);
}
