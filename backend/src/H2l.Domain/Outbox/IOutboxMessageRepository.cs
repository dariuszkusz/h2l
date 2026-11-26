namespace H2l.Domain.Outbox;

public interface IOutboxMessageRepository
{
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OutboxMessage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> ListPendingBatchIgnoringTenantFilterAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeadLetterRecord>> ListDeadLettersAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
