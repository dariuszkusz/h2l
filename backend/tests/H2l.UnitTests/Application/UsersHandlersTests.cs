using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Application.Users.Commands;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class UsersHandlersTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IGroupRepository _groupRepository = Substitute.For<IGroupRepository>();
    private readonly IUserActivationTokenRepository _activationRepo = Substitute.For<IUserActivationTokenRepository>();
    private readonly IInstanceSettingsRepository _settingsRepo = Substitute.For<IInstanceSettingsRepository>();
    private readonly IAuditLogRepository _auditRepo = Substitute.For<IAuditLogRepository>();
    private readonly IEntitlementService _entitlementService = Substitute.For<IEntitlementService>();
    private readonly IOpaqueTokenGenerator _tokenGenerator = Substitute.For<IOpaqueTokenGenerator>();
    private readonly IAesGcmEncryptionService _encryptionService = Substitute.For<IAesGcmEncryptionService>();
    private readonly ISmtpInviteSender _smtpSender = Substitute.For<ISmtpInviteSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public UsersHandlersTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserContext.IsAuthenticated.Returns(true);
        _currentUserContext.UserId.Returns(Guid.CreateVersion7());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _tokenGenerator.GenerateToken().Returns("one-time-token");
        _tokenGenerator.ComputeSha256Hash("one-time-token").Returns("hashed-one-time-token");
    }

    [Fact]
    public async Task CreateUser_WithoutSmtp_ReturnsManualActivationLink()
    {
        var settings = InstanceSettings.Create(_tenantId, "slate", "pl").Value;
        _settingsRepo.GetByTenantIdAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(settings);

        var sut = new CreateUserCommandHandler(
            _tenantContext, _currentUserContext, _userRepository, _groupRepository,
            _activationRepo, _settingsRepo, _auditRepo, _entitlementService,
            _tokenGenerator, _encryptionService, _smtpSender, _unitOfWork, _dateTimeProvider);

        var result = await sut.Handle(
            new CreateUserCommand("newuser@example.com", "ext-1", SystemRole.EndUser, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("manual_link", result.Value.DeliveryMethod);
        Assert.Equal("/activate?token=one-time-token", result.Value.ActivationLink);
        Assert.False(result.Value.IsActive);
        await _smtpSender.DidNotReceiveWithAnyArgs().SendActivationInviteAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task CreateUser_AdminWhenLimitExceeded_ReturnsPaymentRequired()
    {
        _entitlementService.CanCreateAdminUserAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.PaymentRequired("ADMIN_LIMIT_EXCEEDED", "Limit exceeded.")));

        var sut = new CreateUserCommandHandler(
            _tenantContext, _currentUserContext, _userRepository, _groupRepository,
            _activationRepo, _settingsRepo, _auditRepo, _entitlementService,
            _tokenGenerator, _encryptionService, _smtpSender, _unitOfWork, _dateTimeProvider);

        var result = await sut.Handle(
            new CreateUserCommand("secondadmin@example.com", null, SystemRole.Admin, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, result.Error.Type);
        Assert.Equal("ADMIN_LIMIT_EXCEEDED", result.Error.Code);
    }

    [Fact]
    public async Task UpdatePreferences_UpdatesLanguageAndThemeIndependently()
    {
        var user = User.Create(_tenantId, "user@example.com", "hash", SystemRole.EndUser, "en", "light").Value;
        _currentUserContext.UserId.Returns(user.Id);
        _userRepository.GetByIdForUpdateAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sut = new UpdateUserPreferencesCommandHandler(_currentUserContext, _userRepository, _unitOfWork);

        var r1 = await sut.Handle(new UpdateUserPreferencesCommand("de", null), CancellationToken.None);
        Assert.True(r1.IsSuccess);
        Assert.Equal("de", user.PreferredLanguage);
        Assert.Equal("light", user.PreferredTheme);

        var r2 = await sut.Handle(new UpdateUserPreferencesCommand(null, "high-contrast"), CancellationToken.None);
        Assert.True(r2.IsSuccess);
        Assert.Equal("de", user.PreferredLanguage);
        Assert.Equal("high-contrast", user.PreferredTheme);
    }
}
