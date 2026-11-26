using H2l.Application.Auth.Commands;
using H2l.Application.Auth.DTOs;
using H2l.Application.Auth.Mappings;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using Mapster;
using MapsterMapper;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class AuthHandlersTests
{
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public AuthHandlersTests()
    {
        var config = new TypeAdapterConfig();
        new AuthMappingConfig().Register(config);
        _mapper = new Mapper(config);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCredentials_IssuesTokensAndAuditLog()
    {
        var tenantId = Guid.CreateVersion7();
        var user = User.Create(tenantId, "user@example.com", "stored-hash", SystemRole.EndUser, "de", "dark").Value;
        var userRepo = Substitute.For<IUserRepository>();
        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var jwtGen = Substitute.For<IJwtTokenGenerator>();
        var tokenGen = Substitute.For<IOpaqueTokenGenerator>();
        var uow = Substitute.For<IUnitOfWork>();

        userRepo.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        hasher.Verify("CorrectPass!123", "stored-hash").Returns(true);
        jwtGen.GenerateAccessToken(user).Returns(new GeneratedAccessToken("jwt-token", 900, 7));
        tokenGen.GenerateToken().Returns("raw-refresh-token");
        tokenGen.ComputeSha256Hash("raw-refresh-token").Returns("hashed-refresh-token");

        var sut = new LoginCommandHandler(
            userRepo, refreshRepo, auditRepo, hasher, jwtGen, tokenGen, uow, _dateTimeProvider, _mapper);

        var result = await sut.Handle(new LoginCommand("user@example.com", "CorrectPass!123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.AccessToken);
        Assert.Equal("raw-refresh-token", result.Value.RefreshToken);
        Assert.Equal("de", result.Value.User.PreferredLanguage);
        Assert.Equal("dark", result.Value.User.PreferredTheme);
        await refreshRepo.Received(1).AddAsync(Arg.Is<RefreshToken>(t => t.TokenHash == "hashed-refresh-token"), Arg.Any<CancellationToken>());
        await auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.Action == "USER_LOGGED_IN"), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_WithActiveToken_RotatesRefreshToken()
    {
        var now = _dateTimeProvider.UtcNow;
        var tenantId = Guid.CreateVersion7();
        var user = User.Create(tenantId, "user@example.com", "stored-hash", SystemRole.EndUser).Value;
        var oldToken = RefreshToken.Create(tenantId, user.Id, "old-hash", now.AddDays(5), now.AddDays(-1)).Value;

        var refreshRepo = Substitute.For<IRefreshTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var jwtGen = Substitute.For<IJwtTokenGenerator>();
        var tokenGen = Substitute.For<IOpaqueTokenGenerator>();
        var uow = Substitute.For<IUnitOfWork>();

        tokenGen.ComputeSha256Hash("old-raw").Returns("old-hash");
        tokenGen.GenerateToken().Returns("new-raw");
        tokenGen.ComputeSha256Hash("new-raw").Returns("new-hash");
        refreshRepo.GetByTokenHashForUpdateAsync("old-hash", Arg.Any<CancellationToken>()).Returns(oldToken);
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        jwtGen.GenerateAccessToken(user).Returns(new GeneratedAccessToken("new-jwt", 900, 7));

        var sut = new RefreshTokenCommandHandler(refreshRepo, userRepo, jwtGen, tokenGen, uow, _dateTimeProvider, _mapper);
        var result = await sut.Handle(new RefreshTokenCommand("old-raw"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-jwt", result.Value.AccessToken);
        Assert.Equal("new-raw", result.Value.RefreshToken);
        Assert.NotNull(oldToken.RevokedAt);
        Assert.NotNull(oldToken.ReplacedByTokenId);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateAccount_WithValidToken_ActivatesUserAndMarksTokenUsed()
    {
        var now = _dateTimeProvider.UtcNow;
        var tenantId = Guid.CreateVersion7();
        var user = User.CreateInvited(tenantId, "invited@example.com", SystemRole.EndUser).Value;
        var token = UserActivationToken.Create(tenantId, user.Id, "act-hash", now.AddHours(24), now.AddHours(-1)).Value;

        var actRepo = Substitute.For<IUserActivationTokenRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var tokenGen = Substitute.For<IOpaqueTokenGenerator>();
        var hasher = Substitute.For<IPasswordHasher>();
        var uow = Substitute.For<IUnitOfWork>();

        tokenGen.ComputeSha256Hash("raw-act").Returns("act-hash");
        actRepo.GetByTokenHashForUpdateAsync("act-hash", Arg.Any<CancellationToken>()).Returns(token);
        userRepo.GetByIdForUpdateAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        hasher.Hash("NewStrongPass!123").Returns("hashed-new-pass");

        var sut = new ActivateAccountCommandHandler(actRepo, userRepo, auditRepo, tokenGen, hasher, uow, _dateTimeProvider);
        var result = await sut.Handle(new ActivateAccountCommand("raw-act", "NewStrongPass!123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Equal("hashed-new-pass", user.PasswordHash);
        Assert.NotNull(token.UsedAt);
        await auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.Action == "USER_ACTIVATED"), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
