using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Licensing;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Licensing;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Security;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Licensing;

public sealed class SelfHostedEntitlementServiceTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IInstanceSettingsRepository _settingsRepository = Substitute.For<IInstanceSettingsRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IWorkflowProcessRepository _processRepository = Substitute.For<IWorkflowProcessRepository>();
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly TestEd25519LicenseSigner _signer = new();
    private readonly ILicenseVerifier _licenseVerifier;
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public SelfHostedEntitlementServiceTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _encryptionService = new AesGcmEncryptionService(Options.Create(new EncryptionOptions
        {
            KeyId = "v1",
            AesGcmKey = Convert.ToBase64String(new byte[32])
        }));
        _licenseVerifier = new Ed25519LicenseVerifier(_signer);
    }

    private SelfHostedEntitlementService CreateSut()
        => new(
            _tenantContext,
            _settingsRepository,
            _userRepository,
            _processRepository,
            _encryptionService,
            _licenseVerifier,
            _dateTimeProvider);

    [Fact]
    public async Task FreePlan_EnforcesOneActiveProcessAndOneAdminWithUnlimitedEndUsers()
    {
        var settings = InstanceSettings.Create(_tenantId).Value;
        _settingsRepository.GetByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(settings);
        _processRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(1);
        _userRepository.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);
        _userRepository.CountActiveEndUsersAsync(Arg.Any<CancellationToken>()).Returns(250);

        var sut = CreateSut();

        var statusResult = await sut.GetStatusAsync();
        Assert.True(statusResult.IsSuccess);
        Assert.Equal(LicensePlan.Free, statusResult.Value.Plan);
        Assert.Equal(1, statusResult.Value.Limits.MaxActiveProcesses);
        Assert.Equal(1, statusResult.Value.Limits.MaxAdminUsers);
        Assert.Null(statusResult.Value.Limits.MaxEndUsers);
        Assert.Equal(250, statusResult.Value.Usage.EndUsers);

        var processCheck = await sut.CanActivateWorkflowProcessAsync();
        Assert.True(processCheck.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, processCheck.Error.Type);

        var adminCheck = await sut.CanCreateAdminUserAsync();
        Assert.True(adminCheck.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, adminCheck.Error.Type);
    }

    [Fact]
    public async Task ValidProLicense_UnlocksAdditionalProcessesAndAdmins()
    {
        var now = _dateTimeProvider.UtcNow;
        var settings = InstanceSettings.Create(_tenantId).Value;
        var key = _signer.Sign(new LicensePayload(
            Guid.CreateVersion7(),
            settings.InstanceId,
            LicensePlan.Pro,
            MaxActiveProcesses: 10,
            MaxActiveConnectors: 5,
            MaxAdminUsers: 5,
            MaxEndUsers: null,
            IssuedAt: now.AddDays(-1),
            ExpiresAt: now.AddDays(30),
            GracePeriodDays: 14));

        settings.SetEncryptedLicenseKey(_encryptionService.Encrypt(key, $"license:{_tenantId:N}"));
        _settingsRepository.GetByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(settings);
        _processRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(3);
        _userRepository.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(2);
        _userRepository.CountActiveEndUsersAsync(Arg.Any<CancellationToken>()).Returns(50);

        var sut = CreateSut();

        var statusResult = await sut.GetStatusAsync();
        Assert.True(statusResult.IsSuccess);
        Assert.Equal(LicensePlan.Pro, statusResult.Value.Plan);
        Assert.Equal(LicenseStatus.Active, statusResult.Value.Status);
        Assert.True((await sut.CanActivateWorkflowProcessAsync()).IsSuccess);
        Assert.True((await sut.CanCreateAdminUserAsync()).IsSuccess);
    }

    [Fact]
    public async Task ExpiredLicensePastGracePeriod_SoftDegradesToFreeLimits()
    {
        var now = _dateTimeProvider.UtcNow;
        var settings = InstanceSettings.Create(_tenantId).Value;
        var key = _signer.Sign(new LicensePayload(
            Guid.CreateVersion7(),
            settings.InstanceId,
            LicensePlan.Pro,
            MaxActiveProcesses: 10,
            MaxActiveConnectors: 5,
            MaxAdminUsers: 5,
            MaxEndUsers: null,
            IssuedAt: now.AddDays(-60),
            ExpiresAt: now.AddDays(-20),
            GracePeriodDays: 14));

        settings.SetEncryptedLicenseKey(_encryptionService.Encrypt(key, $"license:{_tenantId:N}"));
        _settingsRepository.GetByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(settings);
        _processRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(1);
        _userRepository.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);
        _userRepository.CountActiveEndUsersAsync(Arg.Any<CancellationToken>()).Returns(10);

        var sut = CreateSut();

        var statusResult = await sut.GetStatusAsync();
        Assert.True(statusResult.IsSuccess);
        Assert.Equal(LicenseStatus.Expired, statusResult.Value.Status);
        Assert.False(statusResult.Value.InGracePeriod);
        Assert.Equal(1, statusResult.Value.Limits.MaxActiveProcesses);
        Assert.Equal(1, statusResult.Value.Limits.MaxAdminUsers);

        var processCheck = await sut.CanActivateWorkflowProcessAsync();
        Assert.True(processCheck.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, processCheck.Error.Type);
    }
}
