using H2l.Domain.Common;

namespace H2l.Domain.Workflows;

public sealed class Project : AggregateRoot, ITenantScoped, ISoftDeletable
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Project()
    {
    }

    private Project(
        Guid id,
        Guid tenantId,
        string name,
        string? description,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public static Result<Project> Create(
        Guid tenantId,
        string name,
        string? description = null,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<Project>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Project>(Error.Validation("PROJECT_NAME_REQUIRED", "Project name is required."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200)
        {
            return Result.Failure<Project>(Error.Validation("PROJECT_NAME_TOO_LONG", "Project name cannot exceed 200 characters."));
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription is not null && normalizedDescription.Length > 2000)
        {
            return Result.Failure<Project>(Error.Validation("PROJECT_DESCRIPTION_TOO_LONG", "Project description cannot exceed 2000 characters."));
        }

        var project = new Project(
            Guid.CreateVersion7(),
            tenantId,
            trimmedName,
            normalizedDescription,
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(project);
    }

    public void SoftDelete(DateTimeOffset? deletedAt = null)
    {
        DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
    }
}
