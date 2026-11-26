using H2l.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly H2lDbContext _dbContext;

    public AuditLogRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
        => await _dbContext.AuditLogs.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> ListByResourceAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = resourceType.Trim();
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.ResourceType == normalizedType && a.ResourceId == resourceId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);
    }
}
