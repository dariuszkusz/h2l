using H2l.Domain.Licensing;

namespace H2l.Application.Licensing;

public sealed record LicensePayload(
    Guid LicenseId,
    Guid InstanceId,
    LicensePlan Plan,
    int? MaxActiveProcesses,
    int? MaxActiveConnectors,
    int? MaxAdminUsers,
    int? MaxEndUsers,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    int GracePeriodDays = 14);
