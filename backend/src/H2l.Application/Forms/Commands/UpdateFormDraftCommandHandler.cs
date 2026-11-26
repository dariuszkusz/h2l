using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.DTOs;
using H2l.Application.Forms.Schema;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Commands;

public sealed class UpdateFormDraftCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateFormDraftCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IFormDefinitionRepository formDefinitionRepository,
        IFormVersionRepository formVersionRepository,
        IFormSchemaValidator formSchemaValidator,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _formDefinitionRepository = formDefinitionRepository;
        _formVersionRepository = formVersionRepository;
        _formSchemaValidator = formSchemaValidator;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<FormDefinitionDetailResponse>> Handle(
        UpdateFormDraftCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<FormDefinitionDetailResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var form = await _formDefinitionRepository.GetByIdForUpdateAsync(command.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<FormDefinitionDetailResponse>(
                Error.NotFound("FORM_DEFINITION_NOT_FOUND", "Form definition was not found."));
        }

        var schemaValidation = _formSchemaValidator.ParseAndValidate(command.DraftSchema);
        if (schemaValidation.IsFailure)
        {
            return Result.Failure<FormDefinitionDetailResponse>(schemaValidation.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var rawSchema = command.DraftSchema.GetRawText();
        var updateResult = form.UpdateDraft(command.Name, rawSchema, now);
        if (updateResult.IsFailure)
        {
            return Result.Failure<FormDefinitionDetailResponse>(updateResult.Error);
        }

        var auditResult = AuditLog.Create(
            _tenantContext.TenantId.Value,
            _currentUserContext.UserId,
            "FORM_DRAFT_UPDATED",
            "FormDefinition",
            form.Id,
            JsonSerializer.Serialize(new
            {
                workflowProcessId = form.WorkflowProcessId,
                stepKey = form.StepKey,
                stepCount = schemaValidation.Value.Steps.Count
            }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<FormDefinitionDetailResponse>(auditResult.Error);
        }

        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var latestVersionNumber = await _formVersionRepository.GetLatestVersionNumberAsync(form.Id, cancellationToken);

        return Result.Success(new FormDefinitionDetailResponse(
            form.Id,
            form.WorkflowProcessId,
            form.Name,
            form.StepKey,
            form.Status,
            command.DraftSchema.Clone(),
            latestVersionNumber,
            form.UpdatedAt));
    }
}
