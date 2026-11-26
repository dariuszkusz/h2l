using H2l.Domain.Licensing;

namespace H2l.Application.Licensing;

public sealed record VerifiedLicense(
    LicensePayload Payload,
    LicenseStatus Status,
    DateTimeOffset? GracePeriodEndsAt,
    bool InGracePeriod);
