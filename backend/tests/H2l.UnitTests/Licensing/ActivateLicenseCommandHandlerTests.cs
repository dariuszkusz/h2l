using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Application.Licensing.Commands;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Licensing;
using H2l.Infrastructure.Licensing;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Licensing;

public sealed class ActivateLicenseCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IInstanceSettingsRepository _settingsRepo = Substitute.For<IInstanceSettingsRepository>();
    private readonly IAesGcmEncryptionService _encryptionService = Substitute.For<IAesGcmEncryptionService>();
    private readonly IAuditLogRepository _auditRepo = Substitute.For<IAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEntitlementService _entitlementService = Substitute.For<IEntitlementService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TestEd25519LicenseSigner _signer = new();

    public ActivateLicenseCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserContext.UserId.Returns(Guid.CreateVersion7());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_WithValidLicense_EncryptsPersistsAndAuditsWithoutKey()
    {
        var now = _dateTimeProvider.UtcNow;
        var settings = InstanceSettings.Create(_tenantId).Value;
        var licenseKey = _signer.Sign(new LicensePayload(
            Guid.CreateVersion7(),
            settings.InstanceId,
            LicensePlan.Pro,
            10,
            5,
            5,
            null,
            now.AddDays(-1),
            now.AddDays(30)));

        _settingsRepo.GetByTenantIdForUpdateAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(settings);
        _encryptionService.Encrypt(licenseKey, $"license:{_tenantId:N}").Returns("encrypted-license-blob");
        var expectedStatus = new LicensingStatusResponse(
            settings.InstanceId,
            LicensePlan.Pro,
            LicenseStatus.Active,
            now.AddDays(30),
            null,
            false,
            new LicenseUsage(0, 0, 1, 0),
            new LicenseLimits(10, 5, 5, null));
        _entitlementService.GetStatusAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(expectedStatus));

        AuditLog? capturedAudit = null;
        await _auditRepo.AddAsync(Arg.Do<AuditLog>(a => capturedAudit = a), Arg.Any<CancellationToken>());

        var sut = new ActivateLicenseCommandHandler(
            _tenantContext,
            _currentUserContext,
            _settingsRepo,
            new Ed25519LicenseVerifier(_signer),
            _encryptionService,
            _auditRepo,
            _unitOfWork,
            _entitlementService,
            _dateTimeProvider);

        var result = await sut.Handle(new ActivateLicenseCommand(licenseKey), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("encrypted-license-blob", settings.EncryptedLicenseKey);
        Assert.NotNull(capturedAudit);
        Assert.Equal("LICENSE_ACTIVATED", capturedAudit!.Action);
        Assert.DoesNotContain(licenseKey, capturedAudit.MetadataJsonb ?? string.Empty);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
