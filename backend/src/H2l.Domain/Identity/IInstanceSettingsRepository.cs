namespace H2l.Domain.Identity;

public interface IInstanceSettingsRepository
{
    Task<InstanceSettings?> GetPrimaryAsync(CancellationToken cancellationToken = default);

    Task<InstanceSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task AddAsync(InstanceSettings settings, CancellationToken cancellationToken = default);
}
