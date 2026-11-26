using H2l.Domain.Common;

namespace H2l.Domain.Workflows;

public sealed class FormDefinition : AggregateRoot, ITenantScoped, ISoftDeletable
{
    public Guid TenantId { get; private set; }

    public Guid WorkflowProcessId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string StepKey { get; private set; } = string.Empty;

    public FormDefinitionStatus Status { get; private set; }

    public string? DraftSchemaJsonb { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private FormDefinition()
    {
    }

    private FormDefinition(
        Guid id,
        Guid tenantId,
        Guid workflowProcessId,
        string name,
        string stepKey,
        FormDefinitionStatus status,
        string? draftSchemaJsonb,
        DateTimeOffset updatedAt)
        : base(id)
    {
        TenantId = tenantId;
        WorkflowProcessId = workflowProcessId;
        Name = name;
        StepKey = stepKey;
        Status = status;
        DraftSchemaJsonb = draftSchemaJsonb;
        UpdatedAt = updatedAt;
    }

    public static Result<FormDefinition> Create(
        Guid tenantId,
        Guid workflowProcessId,
        string name,
        string stepKey,
        string? draftSchemaJsonb = null,
        FormDefinitionStatus status = FormDefinitionStatus.Draft,
        DateTimeOffset? updatedAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<FormDefinition>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (workflowProcessId == Guid.Empty)
        {
            return Result.Failure<FormDefinition>(Error.Validation("WORKFLOW_PROCESS_ID_REQUIRED", "Workflow process identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<FormDefinition>(Error.Validation("FORM_NAME_REQUIRED", "Form definition name is required."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200)
        {
            return Result.Failure<FormDefinition>(Error.Validation("FORM_NAME_TOO_LONG", "Form definition name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(stepKey))
        {
            return Result.Failure<FormDefinition>(Error.Validation("STEP_KEY_REQUIRED", "Form step key is required."));
        }

        var trimmedStepKey = stepKey.Trim();
        if (trimmedStepKey.Length > 200)
        {
            return Result.Failure<FormDefinition>(Error.Validation("STEP_KEY_TOO_LONG", "Form step key cannot exceed 200 characters."));
        }

        var form = new FormDefinition(
            Guid.CreateVersion7(),
            tenantId,
            workflowProcessId,
            trimmedName,
            trimmedStepKey,
            status,
            draftSchemaJsonb,
            updatedAt ?? DateTimeOffset.UtcNow);

        return Result.Success(form);
    }

    public Result UpdateDraftSchema(string? draftSchemaJsonb, DateTimeOffset? updatedAt = null)
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Validation("FORM_DELETED", "Cannot update a deleted form definition."));
        }

        DraftSchemaJsonb = draftSchemaJsonb;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result UpdateDraft(string? name, string draftSchemaJsonb, DateTimeOffset? updatedAt = null)
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Validation("FORM_DELETED", "Cannot update a deleted form definition."));
        }

        if (string.IsNullOrWhiteSpace(draftSchemaJsonb))
        {
            return Result.Failure(Error.Validation("DRAFT_SCHEMA_REQUIRED", "Draft schema is required."));
        }

        if (name is not null)
        {
            var trimmedName = name.Trim();
            if (trimmedName.Length == 0)
            {
                return Result.Failure(Error.Validation("FORM_NAME_REQUIRED", "Form definition name is required."));
            }

            if (trimmedName.Length > 200)
            {
                return Result.Failure(Error.Validation("FORM_NAME_TOO_LONG", "Form definition name cannot exceed 200 characters."));
            }

            Name = trimmedName;
        }

        DraftSchemaJsonb = draftSchemaJsonb;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result MarkPublished(DateTimeOffset? updatedAt = null)
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Validation("FORM_DELETED", "Cannot publish a deleted form definition."));
        }

        Status = FormDefinitionStatus.Published;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void SoftDelete(DateTimeOffset? deletedAt = null)
    {
        DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
    }
}
