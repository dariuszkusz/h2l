using H2l.Application.Connectors.Contracts;

namespace H2l.Infrastructure.Connectors.Security;

public interface ITokenProvider
{
    Task<string?> GetAuthorizationHeaderValueAsync(
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken = default);
}
