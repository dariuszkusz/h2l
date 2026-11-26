using H2l.Application.Auth.DTOs;
using H2l.Application.Common;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using MapsterMapper;

namespace H2l.Application.Auth.Commands;

public sealed class RefreshTokenCommandHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IOpaqueTokenGenerator opaqueTokenGenerator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _opaqueTokenGenerator = opaqueTokenGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<Result<AuthTokenResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = _opaqueTokenGenerator.ComputeSha256Hash(command.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActiveAt(now))
        {
            return Result.Failure<AuthTokenResponse>(
                Error.Unauthorized("REFRESH_TOKEN_INVALID", "Refresh token is invalid or expired."));
        }

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthTokenResponse>(
                Error.Unauthorized("USER_INACTIVE", "User account is not active."));
        }

        var access = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRawRefreshToken = _opaqueTokenGenerator.GenerateToken();
        var newRefreshTokenHash = _opaqueTokenGenerator.ComputeSha256Hash(newRawRefreshToken);

        var newTokenResult = RefreshToken.Create(
            user.TenantId,
            user.Id,
            newRefreshTokenHash,
            now.AddDays(access.RefreshTokenLifetimeDays),
            now);

        if (newTokenResult.IsFailure)
        {
            return Result.Failure<AuthTokenResponse>(newTokenResult.Error);
        }

        var revokeResult = existingToken.Revoke(now, newTokenResult.Value.Id);
        if (revokeResult.IsFailure)
        {
            return Result.Failure<AuthTokenResponse>(revokeResult.Error);
        }

        await _refreshTokenRepository.AddAsync(newTokenResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = _mapper.Map<UserProfileResponse>(user);
        return Result.Success(new AuthTokenResponse(
            access.AccessToken,
            newRawRefreshToken,
            access.ExpiresInSeconds,
            profile));
    }
}
