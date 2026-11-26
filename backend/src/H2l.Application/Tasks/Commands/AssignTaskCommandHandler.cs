using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Tasks.Commands;

public sealed class AssignTaskCommandHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeTaskNotifier _realtimeTaskNotifier;

    public AssignTaskCommandHandler(
        ICurrentUserContext currentUserContext,
        IHumanTaskRepository humanTaskRepository,
        IFormVersionRepository formVersionRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRealtimeTaskNotifier realtimeTaskNotifier)
    {
        _currentUserContext = currentUserContext;
        _humanTaskRepository = humanTaskRepository;
        _formVersionRepository = formVersionRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _realtimeTaskNotifier = realtimeTaskNotifier;
    }

    public async Task<Result<TaskDetailResponse>> Handle(
        AssignTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = await _humanTaskRepository.GetByIdForUpdateAsync(command.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.NotFound("TASK_NOT_FOUND", "Human task was not found."));
        }

        var targetUser = await _userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (targetUser is null || !targetUser.IsActive)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.NotFound("TARGET_USER_NOT_FOUND", "Target user was not found or is inactive."));
        }

        var formVersion = await _formVersionRepository.GetByIdAsync(task.FormVersionId, cancellationToken);
        if (formVersion is null)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.NotFound("FORM_VERSION_NOT_FOUND", "Form version for this task was not found."));
        }

        var assignResult = task.AssignToUser(targetUser.Id);
        if (assignResult.IsFailure)
        {
            return Result.Failure<TaskDetailResponse>(assignResult.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var auditResult = AuditLog.Create(
            task.TenantId,
            _currentUserContext.UserId,
            "TASK_ASSIGNED",
            "HumanTask",
            task.Id,
            JsonSerializer.Serialize(new { assignedUserId = targetUser.Id }),
            now);

        if (auditResult.IsSuccess)
        {
            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        }

        var saved = await _unitOfWork.TrySaveChangesAsync(cancellationToken);
        if (!saved)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Conflict("TASK_CONCURRENCY_CONFLICT", "Task state was modified concurrently. Please refresh and try again."));
        }

        await _realtimeTaskNotifier.NotifyTaskAssignedAsync(
            task.TenantId,
            targetUser.Id,
            task.Id,
            cancellationToken);

        return Result.Success(TaskDetailResponseMapper.ToDetailResponse(
            task,
            formVersion,
            _currentUserContext.UserId ?? Guid.Empty));
    }
}
