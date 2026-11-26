using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly H2lDbContext _dbContext;

    public TenantRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == normalized, cancellationToken);
    }

    public Task<Tenant?> GetPrimaryAsync(CancellationToken cancellationToken = default)
        => _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => _dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(cancellationToken);

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        => await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
}
