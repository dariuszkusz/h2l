using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Services;
using H2l.Application.Tasks;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Connectors.Commands;

public sealed class CreateInboundHumanTaskCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IFormAccessRuleRepository _formAccessRuleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly ISsrfGuard _ssrfGuard;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeTaskNotifier _realtimeTaskNotifier;

    public CreateInboundHumanTaskCommandHandler(
        ITenantContext tenantContext,
        IWorkflowConnectorRepository connectorRepository,
        IHumanTaskRepository humanTaskRepository,
        IFormVersionRepository formVersionRepository,
        IFormAccessRuleRepository formAccessRuleRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IAuditLogRepository auditLogRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        ISsrfGuard ssrfGuard,
        IAesGcmEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRealtimeTaskNotifier realtimeTaskNotifier)
    {
        _tenantContext = tenantContext;
        _connectorRepository = connectorRepository;
        _humanTaskRepository = humanTaskRepository;
        _formVersionRepository = formVersionRepository;
        _formAccessRuleRepository = formAccessRuleRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _auditLogRepository = auditLogRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _ssrfGuard = ssrfGuard;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _realtimeTaskNotifier = realtimeTaskNotifier;
    }

    public async Task<Result<InboundWorkflowTaskResponse>> Handle(
        CreateInboundHumanTaskCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<InboundWorkflowTaskResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var tenantId = _tenantContext.TenantId.Value;

        var connector = await _connectorRepository.GetByIdAsync(command.ConnectorId, cancellationToken);
        if (connector is null || !connector.IsActive)
        {
            return Result.Failure<InboundWorkflowTaskResponse>(
                Error.NotFound("CONNECTOR_NOT_FOUND", "Active workflow connector was not found."));
        }

        var existingTask = await _humanTaskRepository.GetByExternalTaskIdAsync(
            connector.Id,
            command.ExternalTaskId.Trim(),
            cancellationToken);

        if (existingTask is not null)
        {
            return Result.Success(new InboundWorkflowTaskResponse(
                existingTask.Id,
                existingTask.Status,
                IsDuplicate: true));
        }

        FormVersion? formVersion = command.FormVersion.HasValue
            ? await _formVersionRepository.GetByFormDefinitionAndNumberAsync(
                command.FormId,
                command.FormVersion.Value,
                cancellationToken)
            : await _formVersionRepository.GetLatestByFormDefinitionIdAsync(
                command.FormId,
                cancellationToken);

        if (formVersion is null)
        {
            return Result.Failure<InboundWorkflowTaskResponse>(
                Error.NotFound("H2L_FORM_NOT_FOUND", "Published form version was not found."));
        }

        string? encryptedResumeUrl = null;
        if (!string.IsNullOrWhiteSpace(command.ResumeUrl))
        {
            var ssrfResult = await _ssrfGuard.ValidateUrlAsync(command.ResumeUrl, cancellationToken);
            if (ssrfResult.IsFailure)
            {
                return Result.Failure<InboundWorkflowTaskResponse>(ssrfResult.Error);
            }

            encryptedResumeUrl = _encryptionService.Encrypt(
                command.ResumeUrl.Trim(),
                $"task-resume:{tenantId:N}:{connector.Id:N}");
        }

        var now = _dateTimeProvider.UtcNow;
        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(command.TimeoutIso8601))
        {
            if (!Iso8601DurationParser.TryParse(command.TimeoutIso8601, out var timeoutDuration))
            {
                return Result.Failure<InboundWorkflowTaskResponse>(
                    Error.Validation("TIMEOUT_ISO8601_INVALID", "timeoutIso8601 must be a valid positive ISO-8601 duration (e.g. P3D, PT4H)."));
            }

            expiresAt = now.Add(timeoutDuration);
        }

        Guid? assignedUserId = null;
        var initialStatus = HumanTaskStatus.Pending;

        if (!string.IsNullOrWhiteSpace(command.Assignee))
        {
            var targetUser = await _userRepository.GetByEmailOrExternalIdAsync(
                command.Assignee.Trim(),
                cancellationToken);

            var isAuthorized = targetUser is not null
                && targetUser.IsActive
                && await _taskAccessEvaluator.CanUserAccessFormAsync(
                    targetUser.Id,
                    formVersion.FormDefinitionId,
                    cancellationToken);

            if (isAuthorized && targetUser is not null)
            {
                assignedUserId = targetUser.Id;
                initialStatus = HumanTaskStatus.Pending;
            }
            else if (command.ThrowOnMissingAssignee)
            {
                return Result.Failure<InboundWorkflowTaskResponse>(
                    Error.NotFound("H2L_ASSIGNEE_NOT_FOUND", "Requested assignee was not found or is not authorized for this form."));
            }
            else
            {
                assignedUserId = null;
                initialStatus = HumanTaskStatus.NeedsAssignment;
            }
        }
        else if (command.CandidateGroups is { Count: > 0 })
        {
            var normalizedCodes = command.CandidateGroups
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matchedGroups = normalizedCodes.Count > 0
                ? await _groupRepository.ListByCodesAsync(normalizedCodes, cancellationToken)
                : [];

            var formRules = await _formAccessRuleRepository.ListByFormDefinitionIdAsync(
                formVersion.FormDefinitionId,
                cancellationToken);

            var allowedGroupIds = formRules
                .Where(r => r.GroupId.HasValue)
                .Select(r => r.GroupId!.Value)
                .ToHashSet();

            var anyAuthorizedGroup = matchedGroups.Any(g => allowedGroupIds.Contains(g.Id));
            if (!anyAuthorizedGroup)
            {
                initialStatus = HumanTaskStatus.NeedsAssignment;
            }
        }

        string? prefilledJson = command.PrefilledData is { Count: > 0 }
            ? JsonSerializer.Serialize(command.PrefilledData)
            : null;

        var sensitiveDataMode = command.SensitiveDataMode ?? SensitiveDataMode.ClaimCheck;

        var createResult = HumanTask.Create(
            tenantId,
            formVersion.Id,
            command.ExternalTaskId,
            connector.Id,
            sensitiveDataMode,
            assignedUserId,
            initialStatus,
            prefilledJson,
            encryptedResumeUrl,
            expiresAt,
            now);

        if (createResult.IsFailure)
        {
            return Result.Failure<InboundWorkflowTaskResponse>(createResult.Error);
        }

        var task = createResult.Value;
        await _humanTaskRepository.AddAsync(task, cancellationToken);

        var auditResult = AuditLog.Create(
            tenantId,
            null,
            "TASK_CREATED_FROM_CONNECTOR",
            "HumanTask",
            task.Id,
            JsonSerializer.Serialize(new
            {
                connectorId = connector.Id,
                formVersionId = formVersion.Id,
                status = task.Status.ToString(),
                sensitiveDataMode = task.SensitiveDataMode.ToString()
            }),
            now);

        if (auditResult.IsSuccess)
        {
            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (assignedUserId.HasValue)
        {
            await _realtimeTaskNotifier.NotifyTaskAssignedAsync(
                tenantId,
                assignedUserId.Value,
                task.Id,
                cancellationToken);
        }
        else
        {
            await _realtimeTaskNotifier.NotifyTaskUpdatedAsync(
                tenantId,
                task.Id,
                task.Status == HumanTaskStatus.NeedsAssignment ? "TaskNeedsAssignment" : "TaskCreated",
                cancellationToken);
        }

        return Result.Success(new InboundWorkflowTaskResponse(
            task.Id,
            task.Status,
            IsDuplicate: false));
    }
}
