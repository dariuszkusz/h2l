using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Commands;

public sealed class CreateFormDefinitionCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IWorkflowProcessRepository _workflowProcessRepository;
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateFormDefinitionCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IWorkflowProcessRepository workflowProcessRepository,
        IFormDefinitionRepository formDefinitionRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _workflowProcessRepository = workflowProcessRepository;
        _formDefinitionRepository = formDefinitionRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<FormDefinitionResponse>> Handle(
        CreateFormDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<FormDefinitionResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var process = await _workflowProcessRepository.GetByIdAsync(command.ProcessId, cancellationToken);
        if (process is null)
        {
            return Result.Failure<FormDefinitionResponse>(
                Error.NotFound("WORKFLOW_PROCESS_NOT_FOUND", "Workflow process was not found."));
        }

        if (await _formDefinitionRepository.ExistsByStepKeyAsync(command.ProcessId, command.StepKey, cancellationToken))
        {
            return Result.Failure<FormDefinitionResponse>(
                Error.Conflict(
                    "FORM_STEP_KEY_ALREADY_EXISTS",
                    "A form definition with this step key already exists in the workflow process."));
        }

        string? serializedSchema = null;
        JsonElement? responseSchema = null;
        if (command.DraftSchemaJsonb.HasValue && command.DraftSchemaJsonb.Value.ValueKind != JsonValueKind.Null)
        {
            serializedSchema = command.DraftSchemaJsonb.Value.GetRawText();
            responseSchema = command.DraftSchemaJsonb.Value.Clone();
        }

        var now = _dateTimeProvider.UtcNow;
        var tenantId = _tenantContext.TenantId.Value;
        var formResult = FormDefinition.Create(
            tenantId,
            command.ProcessId,
            command.Name,
            command.StepKey,
            serializedSchema,
            FormDefinitionStatus.Draft,
            now);

        if (formResult.IsFailure)
        {
            return Result.Failure<FormDefinitionResponse>(formResult.Error);
        }

        var form = formResult.Value;
        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "FORM_DEFINITION_CREATED",
            "FormDefinition",
            form.Id,
            JsonSerializer.Serialize(new
            {
                workflowProcessId = form.WorkflowProcessId,
                stepKey = form.StepKey
            }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<FormDefinitionResponse>(auditResult.Error);
        }

        await _formDefinitionRepository.AddAsync(form, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new FormDefinitionResponse(
            form.Id,
            form.WorkflowProcessId,
            form.Name,
            form.StepKey,
            form.Status,
            responseSchema,
            form.UpdatedAt));
    }
}
