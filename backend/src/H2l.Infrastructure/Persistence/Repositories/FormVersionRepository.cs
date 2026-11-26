using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class FormVersionRepository : IFormVersionRepository
{
    private readonly H2lDbContext _dbContext;

    public FormVersionRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FormVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FormVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<FormVersion?> GetByIdIgnoringTenantFilterAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FormVersions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<FormVersion?> GetByFormDefinitionAndNumberAsync(
        Guid formDefinitionId,
        int versionNumber,
        CancellationToken cancellationToken = default)
        => _dbContext.FormVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.FormDefinitionId == formDefinitionId && v.VersionNumber == versionNumber,
                cancellationToken);

    public Task<FormVersion?> GetLatestByFormDefinitionIdAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default)
        => _dbContext.FormVersions
            .AsNoTracking()
            .Where(v => v.FormDefinitionId == formDefinitionId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FormVersion>> ListByFormDefinitionIdAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default)
        => await _dbContext.FormVersions
            .AsNoTracking()
            .Where(v => v.FormDefinitionId == formDefinitionId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

    public Task<int?> GetLatestVersionNumberAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default)
        => _dbContext.FormVersions
            .AsNoTracking()
            .Where(v => v.FormDefinitionId == formDefinitionId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken);

    public async Task AddAsync(FormVersion formVersion, CancellationToken cancellationToken = default)
        => await _dbContext.FormVersions.AddAsync(formVersion, cancellationToken);
}
