namespace H2l.Domain.Common;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
