using H2l.Domain.Common;

namespace H2l.Application.Licensing;

public interface IEntitlementService
{
    Task<Result<LicensingStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<Result> CanCreateAdminUserAsync(CancellationToken cancellationToken = default);

    Task<Result> CanActivateWorkflowProcessAsync(CancellationToken cancellationToken = default);
}
