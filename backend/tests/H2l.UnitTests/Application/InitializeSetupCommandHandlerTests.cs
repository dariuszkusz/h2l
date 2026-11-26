using H2l.Application.Common;
using H2l.Application.Setup.Commands;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class InitializeSetupCommandHandlerTests
{
    private readonly IInstanceSettingsRepository _settingsRepository = Substitute.For<IInstanceSettingsRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAuditLogRepository _auditLogRepository = Substitute.For<IAuditLogRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAesGcmEncryptionService _encryptionService = Substitute.For<IAesGcmEncryptionService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private InitializeSetupCommandHandler CreateSut()
        => new(
            _settingsRepository,
            _userRepository,
            _auditLogRepository,
            _passwordHasher,
            _encryptionService,
            _unitOfWork,
            _dateTimeProvider);

    [Fact]
    public async Task Handle_WhenAlreadyInitialized_ReturnsConflict()
    {
        _userRepository.AnyAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(
            new InitializeSetupCommand("admin@example.com", "StrongPassword!123", "en", "slate", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("INSTANCE_ALREADY_INITIALIZED", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FreshInstanceWithSmtp_EncryptsSmtpCreatesAdminAndLogsAuditWithoutSecrets()
    {
        var tenantId = Guid.CreateVersion7();
        var settings = InstanceSettings.Create(tenantId).Value;
        _userRepository.AnyAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(false);
        _settingsRepository.GetPrimaryAsync(Arg.Any<CancellationToken>()).Returns(settings);
        _settingsRepository.GetByTenantIdForUpdateAsync(tenantId, Arg.Any<CancellationToken>()).Returns(settings);
        _passwordHasher.Hash("StrongPassword!123").Returns("hashed-admin-password");
        _encryptionService.Encrypt(Arg.Any<string>(), $"smtp:{tenantId:N}").Returns("encrypted-smtp-blob");
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        AuditLog? capturedAudit = null;
        await _auditLogRepository.AddAsync(
            Arg.Do<AuditLog>(a => capturedAudit = a),
            Arg.Any<CancellationToken>());

        var smtp = new SmtpSettingsRequest("smtp.example.com", 587, true, "mailer", "super-secret-smtp-pass", "no-reply@example.com", "h2l");
        var result = await CreateSut().Handle(
            new InitializeSetupCommand("admin@example.com", "StrongPassword!123", "pl", "ocean", smtp),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(settings.InstanceId, result.Value.InstanceId);
        Assert.Equal("ocean", settings.SelectedPalette);
        Assert.Equal("pl", settings.DefaultLanguage);
        Assert.Equal("encrypted-smtp-blob", settings.EncryptedSmtpConfig);
        Assert.NotNull(capturedAudit);
        Assert.Equal("INSTANCE_INITIALIZED", capturedAudit!.Action);
        Assert.DoesNotContain("super-secret-smtp-pass", capturedAudit.MetadataJsonb ?? string.Empty);
        Assert.DoesNotContain("StrongPassword!123", capturedAudit.MetadataJsonb ?? string.Empty);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
