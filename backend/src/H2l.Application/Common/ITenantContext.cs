namespace H2l.Application.Common;

public interface ITenantContext
{
    Guid? TenantId { get; }
}
