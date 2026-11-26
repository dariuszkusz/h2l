using H2l.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly H2lDbContext _dbContext;

    public OutboxMessageRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<OutboxMessage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> ListPendingBatchIgnoringTenantFilterAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
        => await _dbContext.OutboxMessages
            .IgnoreQueryFilters()
            .Where(m => m.Status == OutboxMessageStatus.Pending && m.NextAttemptAt <= utcNow)
            .OrderBy(m => m.NextAttemptAt)
            .ThenBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DeadLetterRecord>> ListDeadLettersAsync(
        CancellationToken cancellationToken = default)
    {
        var query =
            from message in _dbContext.OutboxMessages.AsNoTracking()
            where message.Status == OutboxMessageStatus.DeadLetter
            join task in _dbContext.HumanTasks.AsNoTracking()
                on message.HumanTaskId equals task.Id
            join connector in _dbContext.WorkflowConnectors.IgnoreQueryFilters().AsNoTracking()
                on task.ConnectorId equals connector.Id into connectorJoin
            from connector in connectorJoin.DefaultIfEmpty()
            orderby message.CreatedAt descending
            select new DeadLetterRecord(
                message.Id,
                message.HumanTaskId,
                message.FormSubmissionId,
                connector != null ? connector.Name : "Unknown",
                message.MessageType,
                message.RetryCount,
                message.DeadLetterReason ?? "Delivery failed.",
                message.CreatedAt);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        => await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
}
