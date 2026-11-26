using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.DTOs;
using H2l.Application.Forms.Schema;
using H2l.Application.Licensing;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Commands;

public sealed class PublishFormVersionCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IWorkflowProcessRepository _workflowProcessRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly IEntitlementService _entitlementService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishFormVersionCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IFormDefinitionRepository formDefinitionRepository,
        IWorkflowProcessRepository workflowProcessRepository,
        IFormVersionRepository formVersionRepository,
        IFormSchemaValidator formSchemaValidator,
        IEntitlementService entitlementService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _formDefinitionRepository = formDefinitionRepository;
        _workflowProcessRepository = workflowProcessRepository;
        _formVersionRepository = formVersionRepository;
        _formSchemaValidator = formSchemaValidator;
        _entitlementService = entitlementService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<FormVersionResponse>> Handle(
        PublishFormVersionCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<FormVersionResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<FormVersionResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        var form = await _formDefinitionRepository.GetByIdForUpdateAsync(command.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<FormVersionResponse>(
                Error.NotFound("FORM_DEFINITION_NOT_FOUND", "Form definition was not found."));
        }

        if (string.IsNullOrWhiteSpace(form.DraftSchemaJsonb))
        {
            return Result.Failure<FormVersionResponse>(
                Error.Validation("FORM_DRAFT_EMPTY", "Cannot publish a form definition with an empty draft schema."));
        }

        var schemaValidation = _formSchemaValidator.ParseAndValidate(form.DraftSchemaJsonb);
        if (schemaValidation.IsFailure)
        {
            return Result.Failure<FormVersionResponse>(schemaValidation.Error);
        }

        var process = await _workflowProcessRepository.GetByIdForUpdateAsync(form.WorkflowProcessId, cancellationToken);
        if (process is null)
        {
            return Result.Failure<FormVersionResponse>(
                Error.NotFound("WORKFLOW_PROCESS_NOT_FOUND", "Parent workflow process was not found."));
        }

        if (process.Status == WorkflowProcessStatus.Archived)
        {
            return Result.Failure<FormVersionResponse>(
                Error.Conflict("PROCESS_ARCHIVED", "Cannot publish a form belonging to an archived workflow process."));
        }

        if (process.Status != WorkflowProcessStatus.Active)
        {
            var entitlementResult = await _entitlementService.CanActivateWorkflowProcessAsync(cancellationToken);
            if (entitlementResult.IsFailure)
            {
                return Result.Failure<FormVersionResponse>(entitlementResult.Error);
            }

            var activateResult = process.Activate();
            if (activateResult.IsFailure)
            {
                return Result.Failure<FormVersionResponse>(activateResult.Error);
            }
        }

        var latestVersion = await _formVersionRepository.GetLatestVersionNumberAsync(form.Id, cancellationToken);
        var nextVersionNumber = (latestVersion ?? 0) + 1;
        var now = _dateTimeProvider.UtcNow;
        var tenantId = _tenantContext.TenantId.Value;
        var publisherId = _currentUserContext.UserId.Value;

        var versionResult = FormVersion.Create(
            tenantId,
            form.Id,
            nextVersionNumber,
            form.DraftSchemaJsonb,
            publisherId,
            now);

        if (versionResult.IsFailure)
        {
            return Result.Failure<FormVersionResponse>(versionResult.Error);
        }

        var markResult = form.MarkPublished(now);
        if (markResult.IsFailure)
        {
            return Result.Failure<FormVersionResponse>(markResult.Error);
        }

        var version = versionResult.Value;
        var auditResult = AuditLog.Create(
            tenantId,
            publisherId,
            "FORM_VERSION_PUBLISHED",
            "FormVersion",
            version.Id,
            JsonSerializer.Serialize(new
            {
                formDefinitionId = form.Id,
                workflowProcessId = form.WorkflowProcessId,
                versionNumber = version.VersionNumber
            }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<FormVersionResponse>(auditResult.Error);
        }

        await _formVersionRepository.AddAsync(version, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        using var doc = JsonDocument.Parse(version.SchemaJsonb);
        return Result.Success(new FormVersionResponse(
            version.Id,
            version.FormDefinitionId,
            version.VersionNumber,
            version.PublishedAt,
            version.PublishedByUserId,
            doc.RootElement.Clone()));
    }
}
