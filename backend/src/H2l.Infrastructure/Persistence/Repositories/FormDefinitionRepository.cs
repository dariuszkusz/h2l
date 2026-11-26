using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class FormDefinitionRepository : IFormDefinitionRepository
{
    private readonly H2lDbContext _dbContext;

    public FormDefinitionRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FormDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FormDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<FormDefinition?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FormDefinitions
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FormDefinition>> ListByProcessIdAsync(
        Guid workflowProcessId,
        CancellationToken cancellationToken = default)
        => await _dbContext.FormDefinitions
            .AsNoTracking()
            .Where(f => f.WorkflowProcessId == workflowProcessId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByStepKeyAsync(
        Guid workflowProcessId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var trimmedStepKey = stepKey.Trim();
        return _dbContext.FormDefinitions
            .AsNoTracking()
            .AnyAsync(f => f.WorkflowProcessId == workflowProcessId && f.StepKey == trimmedStepKey, cancellationToken);
    }

    public async Task AddAsync(FormDefinition formDefinition, CancellationToken cancellationToken = default)
        => await _dbContext.FormDefinitions.AddAsync(formDefinition, cancellationToken);
}
