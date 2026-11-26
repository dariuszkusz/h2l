using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Auth.Commands;

public sealed class ActivateAccountCommandHandler
{
    private readonly IUserActivationTokenRepository _activationTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ActivateAccountCommandHandler(
        IUserActivationTokenRepository activationTokenRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IOpaqueTokenGenerator opaqueTokenGenerator,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _activationTokenRepository = activationTokenRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _opaqueTokenGenerator = opaqueTokenGenerator;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        ActivateAccountCommand command,
        CancellationToken cancellationToken)
    {
        var tokenHash = _opaqueTokenGenerator.ComputeSha256Hash(command.Token);
        var activationToken = await _activationTokenRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken);
        if (activationToken is null)
        {
            return Result.Failure(
                Error.Validation("ACTIVATION_TOKEN_INVALID", "Activation token is invalid."));
        }

        var now = _dateTimeProvider.UtcNow;
        var markUsedResult = activationToken.MarkUsed(now);
        if (markUsedResult.IsFailure)
        {
            return markUsedResult;
        }

        var user = await _userRepository.GetByIdForUpdateAsync(activationToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(
                Error.Validation("ACTIVATION_USER_NOT_FOUND", "User account for activation token was not found."));
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var activateResult = user.Activate(passwordHash);
        if (activateResult.IsFailure)
        {
            return activateResult;
        }

        var auditResult = AuditLog.Create(
            user.TenantId,
            user.Id,
            "USER_ACTIVATED",
            "User",
            user.Id,
            metadataJsonb: null,
            occurredAt: now);

        if (auditResult.IsFailure)
        {
            return Result.Failure(auditResult.Error);
        }

        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
