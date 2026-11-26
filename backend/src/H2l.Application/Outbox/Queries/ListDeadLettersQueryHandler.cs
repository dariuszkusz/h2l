using H2l.Application.Outbox.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Outbox;

namespace H2l.Application.Outbox.Queries;

public sealed class ListDeadLettersQueryHandler
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;

    public ListDeadLettersQueryHandler(IOutboxMessageRepository outboxMessageRepository)
    {
        _outboxMessageRepository = outboxMessageRepository;
    }

    public async Task<Result<DeadLetterListResponse>> Handle(
        ListDeadLettersQuery query,
        CancellationToken cancellationToken)
    {
        var records = await _outboxMessageRepository.ListDeadLettersAsync(cancellationToken);
        var items = records
            .Select(r => new DeadLetterItemResponse(
                r.Id,
                r.HumanTaskId,
                r.FormSubmissionId,
                r.ConnectorName,
                r.MessageType,
                r.RetryCount,
                r.DeadLetterReason,
                r.CreatedAt))
            .ToList();

        return Result.Success(new DeadLetterListResponse(items));
    }
}
