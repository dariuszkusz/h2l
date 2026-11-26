using H2l.Domain.Common;

namespace H2l.Application.Connectors.Contracts;

public interface ISsrfGuard
{
    Task<Result<Uri>> ValidateUrlAsync(string url, CancellationToken cancellationToken = default);
}
