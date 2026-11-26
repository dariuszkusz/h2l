using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly H2lDbContext _dbContext;

    public ProjectRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Project?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        return _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Name == trimmedName, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
        => await _dbContext.Projects.AddAsync(project, cancellationToken);
}
