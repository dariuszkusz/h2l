using H2l.Domain.Common;

namespace H2l.Domain.Tasks;

public sealed class FormSubmission : AggregateRoot, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid HumanTaskId { get; private set; }

    public Guid FormVersionId { get; private set; }

    public Guid SubmittedByUserId { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string PublicDataJsonb { get; private set; } = "{}";

    public string? EncryptedSensitiveData { get; private set; }

    public DateTimeOffset? RetentionUntil { get; private set; }

    public DateTimeOffset? AnonymizedAt { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    private FormSubmission()
    {
    }

    private FormSubmission(
        Guid id,
        Guid tenantId,
        Guid humanTaskId,
        Guid formVersionId,
        Guid submittedByUserId,
        string idempotencyKey,
        string publicDataJsonb,
        string? encryptedSensitiveData,
        DateTimeOffset? retentionUntil,
        DateTimeOffset submittedAt)
        : base(id)
    {
        TenantId = tenantId;
        HumanTaskId = humanTaskId;
        FormVersionId = formVersionId;
        SubmittedByUserId = submittedByUserId;
        IdempotencyKey = idempotencyKey;
        PublicDataJsonb = publicDataJsonb;
        EncryptedSensitiveData = encryptedSensitiveData;
        RetentionUntil = retentionUntil;
        SubmittedAt = submittedAt;
    }

    public static Result<FormSubmission> Create(
        Guid id,
        Guid tenantId,
        Guid humanTaskId,
        Guid formVersionId,
        Guid submittedByUserId,
        string idempotencyKey,
        string publicDataJsonb,
        string? encryptedSensitiveData,
        DateTimeOffset? retentionUntil,
        DateTimeOffset submittedAt)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<FormSubmission>(Error.Validation("SUBMISSION_ID_REQUIRED", "Submission identifier is required."));
        }

        if (tenantId == Guid.Empty)
        {
            return Result.Failure<FormSubmission>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (humanTaskId == Guid.Empty)
        {
            return Result.Failure<FormSubmission>(Error.Validation("HUMAN_TASK_ID_REQUIRED", "Human task identifier is required."));
        }

        if (formVersionId == Guid.Empty)
        {
            return Result.Failure<FormSubmission>(Error.Validation("FORM_VERSION_ID_REQUIRED", "Form version identifier is required."));
        }

        if (submittedByUserId == Guid.Empty)
        {
            return Result.Failure<FormSubmission>(Error.Validation("SUBMITTED_BY_USER_REQUIRED", "Submitting user identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<FormSubmission>(Error.Validation("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key is required."));
        }

        var trimmedKey = idempotencyKey.Trim();
        if (trimmedKey.Length > 128)
        {
            return Result.Failure<FormSubmission>(Error.Validation("IDEMPOTENCY_KEY_TOO_LONG", "Idempotency-Key cannot exceed 128 characters."));
        }

        if (string.IsNullOrWhiteSpace(publicDataJsonb))
        {
            publicDataJsonb = "{}";
        }

        return Result.Success(new FormSubmission(
            id,
            tenantId,
            humanTaskId,
            formVersionId,
            submittedByUserId,
            trimmedKey,
            publicDataJsonb,
            encryptedSensitiveData,
            retentionUntil,
            submittedAt));
    }
}
