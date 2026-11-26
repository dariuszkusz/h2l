using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class WorkflowProcessRepository : IWorkflowProcessRepository
{
    private readonly H2lDbContext _dbContext;

    public WorkflowProcessRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WorkflowProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.WorkflowProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<WorkflowProcess?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.WorkflowProcesses
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowProcess>> ListByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowProcesses
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByExternalKeyAsync(
        Guid projectId,
        string externalProcessKey,
        CancellationToken cancellationToken = default)
    {
        var trimmedKey = externalProcessKey.Trim();
        return _dbContext.WorkflowProcesses
            .AsNoTracking()
            .AnyAsync(p => p.ProjectId == projectId && p.ExternalProcessKey == trimmedKey, cancellationToken);
    }

    public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        => _dbContext.WorkflowProcesses
            .AsNoTracking()
            .CountAsync(p => p.Status == WorkflowProcessStatus.Active, cancellationToken);

    public async Task AddAsync(WorkflowProcess process, CancellationToken cancellationToken = default)
        => await _dbContext.WorkflowProcesses.AddAsync(process, cancellationToken);
}
