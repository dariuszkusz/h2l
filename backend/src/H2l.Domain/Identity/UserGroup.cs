using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class UserGroup : ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid GroupId { get; private set; }

    private UserGroup()
    {
    }

    private UserGroup(Guid tenantId, Guid userId, Guid groupId)
    {
        TenantId = tenantId;
        UserId = userId;
        GroupId = groupId;
    }

    public static Result<UserGroup> Create(Guid tenantId, Guid userId, Guid groupId)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<UserGroup>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<UserGroup>(Error.Validation("USER_ID_REQUIRED", "User identifier is required."));
        }

        if (groupId == Guid.Empty)
        {
            return Result.Failure<UserGroup>(Error.Validation("GROUP_ID_REQUIRED", "Group identifier is required."));
        }

        return Result.Success(new UserGroup(tenantId, userId, groupId));
    }
}
