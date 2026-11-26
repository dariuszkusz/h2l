using H2l.Application.Common;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Tasks.Commands;

public sealed class ClaimTaskCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeTaskNotifier _realtimeTaskNotifier;

    public ClaimTaskCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IHumanTaskRepository humanTaskRepository,
        IFormVersionRepository formVersionRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        IRealtimeTaskNotifier realtimeTaskNotifier)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _humanTaskRepository = humanTaskRepository;
        _formVersionRepository = formVersionRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _realtimeTaskNotifier = realtimeTaskNotifier;
    }

    public async Task<Result<TaskDetailResponse>> Handle(
        ClaimTaskCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        var task = await _humanTaskRepository.GetByIdForUpdateAsync(command.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.NotFound("TASK_NOT_FOUND", "Human task was not found."));
        }

        var formVersion = await _formVersionRepository.GetByIdAsync(task.FormVersionId, cancellationToken);
        if (formVersion is null)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.NotFound("FORM_VERSION_NOT_FOUND", "Form version for this task was not found."));
        }

        var userId = _currentUserContext.UserId.Value;

        if (task.AssignedUserId is null && task.ClaimedByUserId is not null)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Conflict("TASK_ALREADY_CLAIMED", "This task has already been claimed by another user."));
        }

        var hasAccess = await _taskAccessEvaluator.CanUserAccessTaskAsync(
            userId,
            task,
            formVersion.FormDefinitionId,
            cancellationToken);

        if (!hasAccess)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Forbidden("TASK_ACCESS_DENIED", "You do not have permission to claim this task."));
        }

        var claimResult = task.Claim(userId);
        if (claimResult.IsFailure)
        {
            return Result.Failure<TaskDetailResponse>(claimResult.Error);
        }

        var saved = await _unitOfWork.TrySaveChangesAsync(cancellationToken);
        if (!saved)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Conflict("TASK_ALREADY_CLAIMED", "This task has already been claimed by another user."));
        }

        await _realtimeTaskNotifier.NotifyTaskUpdatedAsync(
            _tenantContext.TenantId.Value,
            task.Id,
            "TaskClaimed",
            cancellationToken);

        return Result.Success(TaskDetailResponseMapper.ToDetailResponse(task, formVersion, userId));
    }
}
