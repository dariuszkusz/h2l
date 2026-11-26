using H2l.Domain.Common;

namespace H2l.Application.Licensing;

public interface ILicenseVerifier
{
    Result<VerifiedLicense> Verify(
        string licenseKey,
        Guid expectedInstanceId,
        DateTimeOffset now);
}
