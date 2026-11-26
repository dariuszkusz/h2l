using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.Schema;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Tasks.Commands;

public sealed class SubmitTaskCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IFormSubmissionRepository _formSubmissionRepository;
    private readonly ISubmissionAttachmentRepository _submissionAttachmentRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IInstanceSettingsRepository _instanceSettingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly IFormSubmissionValidator _formSubmissionValidator;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeTaskNotifier _realtimeTaskNotifier;

    public SubmitTaskCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IHumanTaskRepository humanTaskRepository,
        IFormVersionRepository formVersionRepository,
        IFormSubmissionRepository formSubmissionRepository,
        ISubmissionAttachmentRepository submissionAttachmentRepository,
        IOutboxMessageRepository outboxMessageRepository,
        IInstanceSettingsRepository instanceSettingsRepository,
        IAuditLogRepository auditLogRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IFormSchemaValidator formSchemaValidator,
        IFormSubmissionValidator formSubmissionValidator,
        IAesGcmEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRealtimeTaskNotifier realtimeTaskNotifier)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _humanTaskRepository = humanTaskRepository;
        _formVersionRepository = formVersionRepository;
        _formSubmissionRepository = formSubmissionRepository;
        _submissionAttachmentRepository = submissionAttachmentRepository;
        _outboxMessageRepository = outboxMessageRepository;
        _instanceSettingsRepository = instanceSettingsRepository;
        _auditLogRepository = auditLogRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _formSchemaValidator = formSchemaValidator;
        _formSubmissionValidator = formSubmissionValidator;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _realtimeTaskNotifier = realtimeTaskNotifier;
    }

    public async Task<Result<SubmitTaskResponse>> Handle(
        SubmitTaskCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.Validation("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key header is required."));
        }

        var trimmedIdempotencyKey = command.IdempotencyKey.Trim();
        var userId = _currentUserContext.UserId.Value;
        var tenantId = _tenantContext.TenantId.Value;

        var existingSubmission = await _formSubmissionRepository.GetByTaskAndIdempotencyKeyAsync(
            command.TaskId,
            trimmedIdempotencyKey,
            cancellationToken);

        if (existingSubmission is not null)
        {
            if (existingSubmission.SubmittedByUserId != userId)
            {
                return Result.Failure<SubmitTaskResponse>(
                    Error.Forbidden("TASK_SUBMISSION_ACCESS_DENIED", "You do not have permission to access this submission."));
            }

            return Result.Success(new SubmitTaskResponse(
                existingSubmission.HumanTaskId,
                existingSubmission.Id,
                HumanTaskStatus.Completed,
                existingSubmission.SubmittedAt));
        }

        var task = await _humanTaskRepository.GetByIdForUpdateAsync(command.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.NotFound("TASK_NOT_FOUND", "Human task was not found."));
        }

        var formVersion = await _formVersionRepository.GetByIdAsync(task.FormVersionId, cancellationToken);
        if (formVersion is null)
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.NotFound("FORM_VERSION_NOT_FOUND", "Form version for this task was not found."));
        }

        var hasAccess = await _taskAccessEvaluator.CanUserAccessTaskAsync(
            userId,
            task,
            formVersion.FormDefinitionId,
            cancellationToken);

        if (!hasAccess)
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.Forbidden("TASK_ACCESS_DENIED", "You do not have permission to submit this task."));
        }

        var requestedAttachmentIds = command.AttachmentIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        IReadOnlyList<SubmissionAttachment> attachments = [];
        if (requestedAttachmentIds.Count > 0)
        {
            attachments = await _submissionAttachmentRepository.ListByIdsForTaskForUpdateAsync(
                task.Id,
                requestedAttachmentIds,
                cancellationToken);

            if (attachments.Count != requestedAttachmentIds.Count)
            {
                return Result.Failure<SubmitTaskResponse>(
                    Error.Validation(
                        "ATTACHMENTS_INVALID",
                        "One or more attachment identifiers do not belong to this task."));
            }
        }

        var schemaResult = _formSchemaValidator.ParseAndValidate(formVersion.SchemaJsonb);
        if (schemaResult.IsFailure)
        {
            return Result.Failure<SubmitTaskResponse>(schemaResult.Error);
        }

        IReadOnlyDictionary<string, JsonElement>? prefilledValues = null;
        if (!string.IsNullOrWhiteSpace(task.PrefilledDataJsonb))
        {
            prefilledValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(task.PrefilledDataJsonb);
        }

        var uploadedAttachmentFieldKeys = attachments
            .Select(a => a.FieldVariableKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var validationResult = _formSubmissionValidator.ValidateAndSplit(
            schemaResult.Value,
            command.Values,
            prefilledValues,
            uploadedAttachmentFieldKeys);

        if (validationResult.IsFailure)
        {
            return Result.Failure<SubmitTaskResponse>(validationResult.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var completeResult = task.Complete(userId, now);
        if (completeResult.IsFailure)
        {
            return Result.Failure<SubmitTaskResponse>(completeResult.Error);
        }

        var submissionId = Guid.CreateVersion7();
        var publicJson = JsonSerializer.Serialize(validationResult.Value.PublicValues);

        string? encryptedSensitiveData = null;
        if (validationResult.Value.SensitiveValues.Count > 0)
        {
            var sensitiveJson = JsonSerializer.Serialize(validationResult.Value.SensitiveValues);
            encryptedSensitiveData = _encryptionService.Encrypt(
                sensitiveJson,
                $"submission:{tenantId:N}:{submissionId:N}");
        }

        DateTimeOffset? retentionUntil = null;
        var settings = await _instanceSettingsRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        if (settings is not null && settings.DefaultRetentionDays > 0)
        {
            retentionUntil = now.AddDays(settings.DefaultRetentionDays);
        }

        var submissionResult = FormSubmission.Create(
            submissionId,
            tenantId,
            task.Id,
            formVersion.Id,
            userId,
            trimmedIdempotencyKey,
            publicJson,
            encryptedSensitiveData,
            retentionUntil,
            now);

        if (submissionResult.IsFailure)
        {
            return Result.Failure<SubmitTaskResponse>(submissionResult.Error);
        }

        foreach (var attachment in attachments)
        {
            var attachResult = attachment.AttachToSubmission(submissionId);
            if (attachResult.IsFailure)
            {
                return Result.Failure<SubmitTaskResponse>(attachResult.Error);
            }
        }

        var outboxResult = OutboxMessage.Create(
            tenantId,
            task.Id,
            submissionId,
            "TASK_SUBMITTED",
            now);

        if (outboxResult.IsFailure)
        {
            return Result.Failure<SubmitTaskResponse>(outboxResult.Error);
        }

        var auditResult = AuditLog.Create(
            tenantId,
            userId,
            "TASK_SUBMITTED",
            "FormSubmission",
            submissionId,
            JsonSerializer.Serialize(new
            {
                humanTaskId = task.Id,
                formVersionId = formVersion.Id,
                sensitiveFieldCount = validationResult.Value.SensitiveValues.Count,
                attachmentCount = attachments.Count
            }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<SubmitTaskResponse>(auditResult.Error);
        }

        await _formSubmissionRepository.AddAsync(submissionResult.Value, cancellationToken);
        await _outboxMessageRepository.AddAsync(outboxResult.Value, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);

        var saved = await _unitOfWork.TrySaveChangesAsync(cancellationToken);
        if (!saved)
        {
            return Result.Failure<SubmitTaskResponse>(
                Error.Conflict("TASK_CONCURRENCY_CONFLICT", "Task state was modified concurrently. Please refresh and try again."));
        }

        await _realtimeTaskNotifier.NotifyTaskUpdatedAsync(
            tenantId,
            task.Id,
            "TaskCompleted",
            cancellationToken);

        return Result.Success(new SubmitTaskResponse(
            task.Id,
            submissionId,
            task.Status,
            now));
    }
}
