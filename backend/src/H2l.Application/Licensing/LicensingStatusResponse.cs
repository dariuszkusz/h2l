using H2l.Domain.Licensing;

namespace H2l.Application.Licensing;

public sealed record LicensingStatusResponse(
    Guid InstanceId,
    LicensePlan Plan,
    LicenseStatus Status,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? GracePeriodEndsAt,
    bool InGracePeriod,
    LicenseUsage Usage,
    LicenseLimits Limits);
