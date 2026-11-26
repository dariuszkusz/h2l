using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class Group : AggregateRoot, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    private Group()
    {
    }

    private Group(
        Guid id,
        Guid tenantId,
        string name,
        string code,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        CreatedAt = createdAt;
    }

    public static Result<Group> Create(
        Guid tenantId,
        string name,
        string code,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<Group>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Group>(Error.Validation("GROUP_NAME_REQUIRED", "Group name is required."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200)
        {
            return Result.Failure<Group>(Error.Validation("GROUP_NAME_TOO_LONG", "Group name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Group>(Error.Validation("GROUP_CODE_REQUIRED", "Group code is required."));
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        if (normalizedCode.Length > 100)
        {
            return Result.Failure<Group>(Error.Validation("GROUP_CODE_TOO_LONG", "Group code cannot exceed 100 characters."));
        }

        var group = new Group(
            Guid.CreateVersion7(),
            tenantId,
            trimmedName,
            normalizedCode,
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(group);
    }
}
