using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Outbox;

namespace H2l.Application.Outbox.Commands;

public sealed class RetryDeadLetterCommandHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RetryDeadLetterCommandHandler(
        ICurrentUserContext currentUserContext,
        IOutboxMessageRepository outboxMessageRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUserContext = currentUserContext;
        _outboxMessageRepository = outboxMessageRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        RetryDeadLetterCommand command,
        CancellationToken cancellationToken)
    {
        var message = await _outboxMessageRepository.GetByIdForUpdateAsync(command.OutboxId, cancellationToken);
        if (message is null)
        {
            return Result.Failure(Error.NotFound("OUTBOX_MESSAGE_NOT_FOUND", "Outbox message was not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var retryResult = message.RetryFromDeadLetter(now);
        if (retryResult.IsFailure)
        {
            return retryResult;
        }

        var auditResult = AuditLog.Create(
            message.TenantId,
            _currentUserContext.UserId,
            "OUTBOX_DEAD_LETTER_RETRIED",
            "OutboxMessage",
            message.Id,
            null,
            now);

        if (auditResult.IsSuccess)
        {
            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
