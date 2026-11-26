using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Auth.Commands;

public sealed class LogoutCommandHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutCommandHandler(
        ICurrentUserContext currentUserContext,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        IOpaqueTokenGenerator opaqueTokenGenerator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUserContext = currentUserContext;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _opaqueTokenGenerator = opaqueTokenGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.UserId is null)
        {
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "Authentication is required."));
        }

        var tokenHash = _opaqueTokenGenerator.ComputeSha256Hash(command.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken);
        if (existingToken is null || existingToken.UserId != _currentUserContext.UserId.Value)
        {
            return Result.Success();
        }

        if (existingToken.RevokedAt is null)
        {
            var now = _dateTimeProvider.UtcNow;
            var revokeResult = existingToken.Revoke(now);
            if (revokeResult.IsFailure)
            {
                return revokeResult;
            }

            var auditResult = AuditLog.Create(
                existingToken.TenantId,
                existingToken.UserId,
                "USER_LOGGED_OUT",
                "User",
                existingToken.UserId,
                metadataJsonb: null,
                occurredAt: now);

            if (auditResult.IsFailure)
            {
                return Result.Failure(auditResult.Error);
            }

            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
