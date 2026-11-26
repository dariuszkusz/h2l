namespace H2l.Domain.Audit;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> ListByResourceAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}
