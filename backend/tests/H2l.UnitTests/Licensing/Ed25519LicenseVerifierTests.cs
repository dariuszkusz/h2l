using System.Text;
using H2l.Application.Licensing;
using H2l.Domain.Licensing;
using H2l.Infrastructure.Licensing;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Licensing;

public sealed class Ed25519LicenseVerifierTests
{
    private readonly TestEd25519LicenseSigner _signer = new();
    private readonly Ed25519LicenseVerifier _verifier;

    public Ed25519LicenseVerifierTests()
    {
        _verifier = new Ed25519LicenseVerifier(_signer);
    }

    [Fact]
    public void Verify_WithValidActiveSignatureAndMatchingInstanceId_ReturnsActiveLicense()
    {
        var instanceId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var payload = new LicensePayload(
            Guid.CreateVersion7(),
            instanceId,
            LicensePlan.Pro,
            MaxActiveProcesses: 25,
            MaxActiveConnectors: 10,
            MaxAdminUsers: 5,
            MaxEndUsers: null,
            IssuedAt: now.AddDays(-1),
            ExpiresAt: now.AddDays(30),
            GracePeriodDays: 14);

        var key = _signer.Sign(payload);
        var result = _verifier.Verify(key, instanceId, now);

        Assert.True(result.IsSuccess);
        Assert.Equal(LicensePlan.Pro, result.Value.Payload.Plan);
        Assert.Equal(LicenseStatus.Active, result.Value.Status);
        Assert.False(result.Value.InGracePeriod);
    }

    [Fact]
    public void Verify_WithTamperedPayload_ReturnsSignatureInvalid()
    {
        var instanceId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var payload = new LicensePayload(
            Guid.CreateVersion7(),
            instanceId,
            LicensePlan.Pro,
            10,
            10,
            5,
            null,
            now.AddDays(-1),
            now.AddDays(30));

        var key = _signer.Sign(payload);
        var parts = key.Split('.');
        var tamperedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"plan\":\"Enterprise\"}"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var tamperedKey = $"{parts[0]}.{tamperedPayload}.{parts[2]}";

        var result = _verifier.Verify(tamperedKey, instanceId, now);

        Assert.True(result.IsFailure);
        Assert.Equal("LICENSE_SIGNATURE_INVALID", result.Error.Code);
    }

    [Fact]
    public void Verify_WithDifferentInstanceId_ReturnsInstanceMismatch()
    {
        var licensedInstanceId = Guid.CreateVersion7();
        var otherInstanceId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var payload = new LicensePayload(
            Guid.CreateVersion7(),
            licensedInstanceId,
            LicensePlan.Pro,
            10,
            10,
            5,
            null,
            now.AddDays(-1),
            now.AddDays(30));

        var key = _signer.Sign(payload);
        var result = _verifier.Verify(key, otherInstanceId, now);

        Assert.True(result.IsFailure);
        Assert.Equal("LICENSE_INSTANCE_MISMATCH", result.Error.Code);
    }

    [Fact]
    public void Verify_WithinGracePeriod_ReturnsGracePeriodStatus()
    {
        var instanceId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(-5);
        var payload = new LicensePayload(
            Guid.CreateVersion7(),
            instanceId,
            LicensePlan.Pro,
            10,
            10,
            5,
            null,
            now.AddDays(-40),
            expiresAt,
            GracePeriodDays: 14);

        var key = _signer.Sign(payload);
        var result = _verifier.Verify(key, instanceId, now);

        Assert.True(result.IsSuccess);
        Assert.Equal(LicenseStatus.GracePeriod, result.Value.Status);
        Assert.True(result.Value.InGracePeriod);
        Assert.Equal(expiresAt.AddDays(14), result.Value.GracePeriodEndsAt);
    }

    [Fact]
    public void Verify_AfterGracePeriod_ReturnsExpiredStatus()
    {
        var instanceId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(-20);
        var payload = new LicensePayload(
            Guid.CreateVersion7(),
            instanceId,
            LicensePlan.Enterprise,
            null,
            null,
            null,
            null,
            now.AddDays(-60),
            expiresAt,
            GracePeriodDays: 14);

        var key = _signer.Sign(payload);
        var result = _verifier.Verify(key, instanceId, now);

        Assert.True(result.IsSuccess);
        Assert.Equal(LicenseStatus.Expired, result.Value.Status);
        Assert.False(result.Value.InGracePeriod);
    }

    [Fact]
    public void Verify_WhenPublicKeyNotConfigured_ReturnsValidationError()
    {
        var provider = Substitute.For<ILicensePublicKeyProvider>();
        provider.GetPublicKeyBytes().Returns((byte[]?)null);
        var verifier = new Ed25519LicenseVerifier(provider);

        var result = verifier.Verify("H2L1.abc.def", Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("LICENSE_PUBLIC_KEY_NOT_CONFIGURED", result.Error.Code);
    }
}
