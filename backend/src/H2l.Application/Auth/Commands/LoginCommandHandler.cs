using H2l.Application.Auth.DTOs;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using MapsterMapper;

namespace H2l.Application.Auth.Commands;

public sealed class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOpaqueTokenGenerator opaqueTokenGenerator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _opaqueTokenGenerator = opaqueTokenGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<Result<AuthTokenResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null ||
            !user.IsActive ||
            string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokenResponse>(
                Error.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password."));
        }

        var now = _dateTimeProvider.UtcNow;
        var access = _jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _opaqueTokenGenerator.GenerateToken();
        var refreshTokenHash = _opaqueTokenGenerator.ComputeSha256Hash(rawRefreshToken);

        var refreshTokenResult = RefreshToken.Create(
            user.TenantId,
            user.Id,
            refreshTokenHash,
            now.AddDays(access.RefreshTokenLifetimeDays),
            now);

        if (refreshTokenResult.IsFailure)
        {
            return Result.Failure<AuthTokenResponse>(refreshTokenResult.Error);
        }

        var auditResult = AuditLog.Create(
            user.TenantId,
            user.Id,
            "USER_LOGGED_IN",
            "User",
            user.Id,
            metadataJsonb: null,
            occurredAt: now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<AuthTokenResponse>(auditResult.Error);
        }

        await _refreshTokenRepository.AddAsync(refreshTokenResult.Value, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = _mapper.Map<UserProfileResponse>(user);
        return Result.Success(new AuthTokenResponse(
            access.AccessToken,
            rawRefreshToken,
            access.ExpiresInSeconds,
            profile));
    }
}
