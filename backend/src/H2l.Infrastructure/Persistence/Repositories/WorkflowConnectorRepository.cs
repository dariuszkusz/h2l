using H2l.Domain.Connectors;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class WorkflowConnectorRepository : IWorkflowConnectorRepository
{
    private readonly H2lDbContext _dbContext;

    public WorkflowConnectorRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WorkflowConnector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.WorkflowConnectors
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<WorkflowConnector?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.WorkflowConnectors
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<WorkflowConnector?> GetByIdIgnoringTenantFilterAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.WorkflowConnectors
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<WorkflowConnector>> ListAsync(CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowConnectors
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowConnector>> ListActiveByTypeIgnoringTenantFilterAsync(
        ConnectorType connectorType,
        CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowConnectors
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.DeletedAt == null && c.IsActive && c.ConnectorType == connectorType)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        => _dbContext.WorkflowConnectors
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeConnectorId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return _dbContext.WorkflowConnectors
            .AsNoTracking()
            .AnyAsync(
                c => c.Name.ToLower() == normalized
                     && (!excludeConnectorId.HasValue || c.Id != excludeConnectorId.Value),
                cancellationToken);
    }

    public async Task AddAsync(WorkflowConnector connector, CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowConnectors.AddAsync(connector, cancellationToken);
}
