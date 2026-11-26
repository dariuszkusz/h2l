using H2l.Domain.Common;

namespace H2l.Domain.Tasks;

public sealed class SubmissionAttachment : Entity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid HumanTaskId { get; private set; }

    public Guid? FormSubmissionId { get; private set; }

    public string FieldVariableKey { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string StoragePath { get; private set; } = string.Empty;

    public bool IsEncrypted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private SubmissionAttachment()
    {
    }

    private SubmissionAttachment(
        Guid id,
        Guid tenantId,
        Guid humanTaskId,
        string fieldVariableKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storagePath,
        bool isEncrypted,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        HumanTaskId = humanTaskId;
        FieldVariableKey = fieldVariableKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StoragePath = storagePath;
        IsEncrypted = isEncrypted;
        CreatedAt = createdAt;
    }

    public static Result<SubmissionAttachment> Create(
        Guid id,
        Guid tenantId,
        Guid humanTaskId,
        string fieldVariableKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storagePath,
        bool isEncrypted,
        DateTimeOffset? createdAt = null)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("ATTACHMENT_ID_REQUIRED", "Attachment identifier is required."));
        }

        if (tenantId == Guid.Empty)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (humanTaskId == Guid.Empty)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("HUMAN_TASK_ID_REQUIRED", "Human task identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(fieldVariableKey) || fieldVariableKey.Trim().Length > 64)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("FIELD_VARIABLE_KEY_INVALID", "Field variable key is invalid."));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("FILE_NAME_REQUIRED", "Original file name is required."));
        }

        var sanitizedFileName = Path.GetFileName(originalFileName.Trim());
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("FILE_NAME_INVALID", "Original file name is invalid."));
        }

        if (sanitizedFileName.Length > 255)
        {
            sanitizedFileName = sanitizedFileName[..255];
        }

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Trim().Length > 128)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("CONTENT_TYPE_INVALID", "Content type is invalid."));
        }

        if (sizeBytes <= 0)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("FILE_EMPTY", "Uploaded file must not be empty."));
        }

        if (string.IsNullOrWhiteSpace(storagePath) || storagePath.Trim().Length > 512)
        {
            return Result.Failure<SubmissionAttachment>(Error.Validation("STORAGE_PATH_INVALID", "Storage path is invalid."));
        }

        return Result.Success(new SubmissionAttachment(
            id,
            tenantId,
            humanTaskId,
            fieldVariableKey.Trim(),
            sanitizedFileName,
            contentType.Trim(),
            sizeBytes,
            storagePath.Trim(),
            isEncrypted,
            createdAt ?? DateTimeOffset.UtcNow));
    }

    public Result AttachToSubmission(Guid formSubmissionId)
    {
        if (formSubmissionId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("SUBMISSION_ID_REQUIRED", "Form submission identifier is required."));
        }

        if (FormSubmissionId.HasValue && FormSubmissionId.Value != formSubmissionId)
        {
            return Result.Failure(Error.Conflict(
                "ATTACHMENT_ALREADY_LINKED",
                "Attachment is already linked to another form submission."));
        }

        FormSubmissionId = formSubmissionId;
        return Result.Success();
    }
}
