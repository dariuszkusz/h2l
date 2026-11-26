using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Application.Projects.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Workflows;
using MapsterMapper;

namespace H2l.Application.Projects.Commands;

public sealed class CreateWorkflowProcessCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkflowProcessRepository _workflowProcessRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public CreateWorkflowProcessCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IProjectRepository projectRepository,
        IWorkflowProcessRepository workflowProcessRepository,
        IAuditLogRepository auditLogRepository,
        IEntitlementService entitlementService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _projectRepository = projectRepository;
        _workflowProcessRepository = workflowProcessRepository;
        _auditLogRepository = auditLogRepository;
        _entitlementService = entitlementService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<Result<WorkflowProcessResponse>> Handle(
        CreateWorkflowProcessCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<WorkflowProcessResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<WorkflowProcessResponse>(
                Error.NotFound("PROJECT_NOT_FOUND", "Project was not found."));
        }

        var targetStatus = command.Status ?? WorkflowProcessStatus.Draft;
        if (targetStatus == WorkflowProcessStatus.Active)
        {
            var entitlementResult = await _entitlementService.CanActivateWorkflowProcessAsync(cancellationToken);
            if (entitlementResult.IsFailure)
            {
                return Result.Failure<WorkflowProcessResponse>(entitlementResult.Error);
            }
        }

        if (await _workflowProcessRepository.ExistsByExternalKeyAsync(
                command.ProjectId,
                command.ExternalProcessKey,
                cancellationToken))
        {
            return Result.Failure<WorkflowProcessResponse>(
                Error.Conflict(
                    "EXTERNAL_PROCESS_KEY_ALREADY_EXISTS",
                    "A workflow process with this external process key already exists in the project."));
        }

        var now = _dateTimeProvider.UtcNow;
        var tenantId = _tenantContext.TenantId.Value;
        var processResult = WorkflowProcess.Create(
            tenantId,
            command.ProjectId,
            command.Name,
            command.ExternalProcessKey,
            command.ConnectorId,
            targetStatus,
            now);

        if (processResult.IsFailure)
        {
            return Result.Failure<WorkflowProcessResponse>(processResult.Error);
        }

        var process = processResult.Value;
        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "WORKFLOW_PROCESS_CREATED",
            "WorkflowProcess",
            process.Id,
            JsonSerializer.Serialize(new
            {
                projectId = process.ProjectId,
                externalProcessKey = process.ExternalProcessKey,
                status = process.Status.ToString()
            }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<WorkflowProcessResponse>(auditResult.Error);
        }

        await _workflowProcessRepository.AddAsync(process, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.Map<WorkflowProcessResponse>(process));
    }
}
