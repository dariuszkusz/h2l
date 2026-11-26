using H2l.Domain.Common;

namespace H2l.Application.Licensing.Queries;

public sealed class GetLicensingStatusQueryHandler
{
    private readonly IEntitlementService _entitlementService;

    public GetLicensingStatusQueryHandler(IEntitlementService entitlementService)
    {
        _entitlementService = entitlementService;
    }

    public Task<Result<LicensingStatusResponse>> Handle(
        GetLicensingStatusQuery query,
        CancellationToken cancellationToken)
        => _entitlementService.GetStatusAsync(cancellationToken);
}
