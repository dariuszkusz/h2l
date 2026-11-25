using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class InstanceSettingsRepository : IInstanceSettingsRepository
{
    private readonly H2lDbContext _dbContext;

    public InstanceSettingsRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InstanceSettings?> GetPrimaryAsync(CancellationToken cancellationToken = default)
        => _dbContext.InstanceSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<InstanceSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _dbContext.InstanceSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    public async Task AddAsync(InstanceSettings settings, CancellationToken cancellationToken = default)
        => await _dbContext.InstanceSettings.AddAsync(settings, cancellationToken);
}
