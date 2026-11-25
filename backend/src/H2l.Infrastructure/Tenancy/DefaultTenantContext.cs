using H2l.Application.Common;

namespace H2l.Infrastructure.Tenancy;

public sealed class DefaultTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}
