using H2l.Domain.Common;

namespace H2l.Domain.Workflows;

public sealed class FormAccessRule : Entity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid FormDefinitionId { get; private set; }

    public Guid? GroupId { get; private set; }

    public Guid? UserId { get; private set; }

    public FormAccessScope AccessScope { get; private set; }

    private FormAccessRule()
    {
    }

    private FormAccessRule(
        Guid id,
        Guid tenantId,
        Guid formDefinitionId,
        Guid? groupId,
        Guid? userId,
        FormAccessScope accessScope)
        : base(id)
    {
        TenantId = tenantId;
        FormDefinitionId = formDefinitionId;
        GroupId = groupId;
        UserId = userId;
        AccessScope = accessScope;
    }

    public static Result<FormAccessRule> Create(
        Guid tenantId,
        Guid formDefinitionId,
        Guid? groupId,
        Guid? userId)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<FormAccessRule>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (formDefinitionId == Guid.Empty)
        {
            return Result.Failure<FormAccessRule>(Error.Validation("FORM_DEFINITION_ID_REQUIRED", "Form definition identifier is required."));
        }

        if (groupId == Guid.Empty)
        {
            return Result.Failure<FormAccessRule>(Error.Validation("GROUP_ID_INVALID", "Group identifier cannot be empty when specified."));
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<FormAccessRule>(Error.Validation("USER_ID_INVALID", "User identifier cannot be empty when specified."));
        }

        if (groupId is null && userId is null)
        {
            return Result.Failure<FormAccessRule>(
                Error.Validation("FORM_ACCESS_RULE_TARGET_REQUIRED", "At least one of groupId or userId must be specified."));
        }

        var accessScope = (groupId, userId) switch
        {
            (not null, null) => FormAccessScope.WholeGroup,
            (not null, not null) => FormAccessScope.GroupUserSubset,
            (null, not null) => FormAccessScope.DirectUser,
            _ => throw new InvalidOperationException("Unreachable form access rule scope combination.")
        };

        var rule = new FormAccessRule(
            Guid.CreateVersion7(),
            tenantId,
            formDefinitionId,
            groupId,
            userId,
            accessScope);

        return Result.Success(rule);
    }
}
