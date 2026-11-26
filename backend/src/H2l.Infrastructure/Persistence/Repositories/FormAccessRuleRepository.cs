using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class FormAccessRuleRepository : IFormAccessRuleRepository
{
    private readonly H2lDbContext _dbContext;

    public FormAccessRuleRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FormAccessRule>> ListByFormDefinitionIdAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default)
        => await _dbContext.FormAccessRules
            .AsNoTracking()
            .Where(r => r.FormDefinitionId == formDefinitionId)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FormAccessRule>> ListByFormDefinitionIdForUpdateAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default)
        => await _dbContext.FormAccessRules
            .Where(r => r.FormDefinitionId == formDefinitionId)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<FormAccessRule> rules, CancellationToken cancellationToken = default)
        => await _dbContext.FormAccessRules.AddRangeAsync(rules, cancellationToken);

    public void RemoveRange(IEnumerable<FormAccessRule> rules)
        => _dbContext.FormAccessRules.RemoveRange(rules);
}
