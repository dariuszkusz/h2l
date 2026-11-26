using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Projects.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Workflows;
using MapsterMapper;

namespace H2l.Application.Projects.Commands;

public sealed class CreateProjectCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IProjectRepository _projectRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public CreateProjectCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IProjectRepository projectRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _projectRepository = projectRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<Result<ProjectResponse>> Handle(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<ProjectResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (await _projectRepository.ExistsByNameAsync(command.Name, cancellationToken))
        {
            return Result.Failure<ProjectResponse>(
                Error.Conflict("PROJECT_NAME_ALREADY_EXISTS", "A project with this name already exists."));
        }

        var now = _dateTimeProvider.UtcNow;
        var tenantId = _tenantContext.TenantId.Value;
        var projectResult = Project.Create(tenantId, command.Name, command.Description, now);
        if (projectResult.IsFailure)
        {
            return Result.Failure<ProjectResponse>(projectResult.Error);
        }

        var project = projectResult.Value;
        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "PROJECT_CREATED",
            "Project",
            project.Id,
            JsonSerializer.Serialize(new { name = project.Name }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<ProjectResponse>(auditResult.Error);
        }

        await _projectRepository.AddAsync(project, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.Map<ProjectResponse>(project));
    }
}
