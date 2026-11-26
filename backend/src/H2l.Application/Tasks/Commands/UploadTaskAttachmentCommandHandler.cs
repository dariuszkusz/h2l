using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.Schema;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Tasks.Commands;

public sealed class UploadTaskAttachmentCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly ISubmissionAttachmentRepository _submissionAttachmentRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly IFormSubmissionValidator _formSubmissionValidator;
    private readonly IAttachmentStorageService _attachmentStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UploadTaskAttachmentCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IHumanTaskRepository humanTaskRepository,
        IFormVersionRepository formVersionRepository,
        ISubmissionAttachmentRepository submissionAttachmentRepository,
        IAuditLogRepository auditLogRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IFormSchemaValidator formSchemaValidator,
        IFormSubmissionValidator formSubmissionValidator,
        IAttachmentStorageService attachmentStorageService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _humanTaskRepository = humanTaskRepository;
        _formVersionRepository = formVersionRepository;
        _submissionAttachmentRepository = submissionAttachmentRepository;
        _auditLogRepository = auditLogRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _formSchemaValidator = formSchemaValidator;
        _formSubmissionValidator = formSubmissionValidator;
        _attachmentStorageService = attachmentStorageService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AttachmentUploadResponse>> Handle(
        UploadTaskAttachmentCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        var task = await _humanTaskRepository.GetByIdAsync(command.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.NotFound("TASK_NOT_FOUND", "Human task was not found."));
        }

        if (task.Status is not (HumanTaskStatus.Pending or HumanTaskStatus.InProgress))
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.Conflict("TASK_NOT_OPEN", $"Cannot upload attachments to a task in status '{task.Status}'."));
        }

        var formVersion = await _formVersionRepository.GetByIdAsync(task.FormVersionId, cancellationToken);
        if (formVersion is null)
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.NotFound("FORM_VERSION_NOT_FOUND", "Form version for this task was not found."));
        }

        var userId = _currentUserContext.UserId.Value;
        var hasAccess = await _taskAccessEvaluator.CanUserAccessTaskAsync(
            userId,
            task,
            formVersion.FormDefinitionId,
            cancellationToken);

        if (!hasAccess)
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.Forbidden("TASK_ACCESS_DENIED", "You do not have permission to upload attachments for this task."));
        }

        var schemaResult = _formSchemaValidator.ParseAndValidate(formVersion.SchemaJsonb);
        if (schemaResult.IsFailure)
        {
            return Result.Failure<AttachmentUploadResponse>(schemaResult.Error);
        }

        var component = _formSubmissionValidator.FindComponentByVariableKey(
            schemaResult.Value,
            command.FieldVariableKey);

        if (component is null || !string.Equals(component.Type, "file", StringComparison.Ordinal))
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.Validation(
                    "ATTACHMENT_FIELD_INVALID",
                    $"Field '{command.FieldVariableKey}' is not a file upload component in this form version."));
        }

        var maxAllowedBytes = _attachmentStorageService.MaxFileSizeBytes;
        if (component.FileConfig?.MaxSizeBytes is > 0 && component.FileConfig.MaxSizeBytes.Value < maxAllowedBytes)
        {
            maxAllowedBytes = component.FileConfig.MaxSizeBytes.Value;
        }

        if (command.SizeBytes > maxAllowedBytes || command.Content.LongLength > maxAllowedBytes)
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.PayloadTooLarge(
                    "ATTACHMENT_TOO_LARGE",
                    $"Attachment exceeds maximum allowed size of {maxAllowedBytes} bytes."));
        }

        var normalizedMime = command.ContentType.Trim();
        var allowedMimes = component.FileConfig?.AllowedMimeTypes is { Count: > 0 }
            ? new HashSet<string>(component.FileConfig.AllowedMimeTypes, StringComparer.OrdinalIgnoreCase)
            : _attachmentStorageService.DefaultAllowedMimeTypes;

        if (!allowedMimes.Contains(normalizedMime))
        {
            return Result.Failure<AttachmentUploadResponse>(
                Error.Validation(
                    "ATTACHMENT_MIME_TYPE_NOT_ALLOWED",
                    $"File MIME type '{normalizedMime}' is not allowed for field '{command.FieldVariableKey}'."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        var attachmentId = Guid.CreateVersion7();
        var stored = await _attachmentStorageService.SaveEncryptedAsync(
            tenantId,
            attachmentId,
            command.Content,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var attachmentResult = SubmissionAttachment.Create(
            attachmentId,
            tenantId,
            task.Id,
            command.FieldVariableKey,
            command.OriginalFileName,
            normalizedMime,
            command.Content.LongLength,
            stored.StoragePath,
            stored.IsEncrypted,
            now);

        if (attachmentResult.IsFailure)
        {
            return Result.Failure<AttachmentUploadResponse>(attachmentResult.Error);
        }

        var attachment = attachmentResult.Value;
        var auditResult = AuditLog.Create(
            tenantId,
            userId,
            "ATTACHMENT_UPLOADED",
            "SubmissionAttachment",
            attachment.Id,
            JsonSerializer.Serialize(new
            {
                humanTaskId = task.Id,
                fieldVariableKey = attachment.FieldVariableKey,
                contentType = attachment.ContentType,
                sizeBytes = attachment.SizeBytes
            }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<AttachmentUploadResponse>(auditResult.Error);
        }

        await _submissionAttachmentRepository.AddAsync(attachment, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AttachmentUploadResponse(
            attachment.Id,
            attachment.FieldVariableKey,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes));
    }
}
