using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class Tenant : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Tenant()
    {
    }

    private Tenant(Guid id, string name, string slug, DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static Result<Tenant> Create(string name, string slug, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tenant>(Error.Validation("TENANT_NAME_REQUIRED", "Tenant name is required."));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result.Failure<Tenant>(Error.Validation("TENANT_SLUG_REQUIRED", "Tenant slug is required."));
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var tenant = new Tenant(
            Guid.CreateVersion7(),
            name.Trim(),
            normalizedSlug,
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(tenant);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
