using H2l.Domain.Identity;

namespace H2l.Application.Common;

public interface ICurrentUserContext
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    SystemRole? SystemRole { get; }

    bool IsAuthenticated { get; }
}
