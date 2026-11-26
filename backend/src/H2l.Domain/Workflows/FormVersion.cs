using H2l.Domain.Common;

namespace H2l.Domain.Workflows;

public sealed class FormVersion : Entity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid FormDefinitionId { get; private set; }

    public int VersionNumber { get; private set; }

    public string SchemaJsonb { get; private set; } = string.Empty;

    public Guid PublishedByUserId { get; private set; }

    public DateTimeOffset PublishedAt { get; private set; }

    private FormVersion()
    {
    }

    private FormVersion(
        Guid id,
        Guid tenantId,
        Guid formDefinitionId,
        int versionNumber,
        string schemaJsonb,
        Guid publishedByUserId,
        DateTimeOffset publishedAt)
        : base(id)
    {
        TenantId = tenantId;
        FormDefinitionId = formDefinitionId;
        VersionNumber = versionNumber;
        SchemaJsonb = schemaJsonb;
        PublishedByUserId = publishedByUserId;
        PublishedAt = publishedAt;
    }

    public static Result<FormVersion> Create(
        Guid tenantId,
        Guid formDefinitionId,
        int versionNumber,
        string schemaJsonb,
        Guid publishedByUserId,
        DateTimeOffset? publishedAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<FormVersion>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (formDefinitionId == Guid.Empty)
        {
            return Result.Failure<FormVersion>(Error.Validation("FORM_DEFINITION_ID_REQUIRED", "Form definition identifier is required."));
        }

        if (versionNumber < 1)
        {
            return Result.Failure<FormVersion>(Error.Validation("VERSION_NUMBER_INVALID", "Version number must be greater than or equal to 1."));
        }

        if (string.IsNullOrWhiteSpace(schemaJsonb))
        {
            return Result.Failure<FormVersion>(Error.Validation("FORM_SCHEMA_REQUIRED", "Form version schema JSON is required."));
        }

        if (publishedByUserId == Guid.Empty)
        {
            return Result.Failure<FormVersion>(Error.Validation("PUBLISHED_BY_USER_REQUIRED", "Publishing user identifier is required."));
        }

        return Result.Success(new FormVersion(
            Guid.CreateVersion7(),
            tenantId,
            formDefinitionId,
            versionNumber,
            schemaJsonb,
            publishedByUserId,
            publishedAt ?? DateTimeOffset.UtcNow));
    }
}
