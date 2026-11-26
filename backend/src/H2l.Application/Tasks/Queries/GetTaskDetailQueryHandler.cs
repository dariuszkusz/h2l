using H2l.Application.Common;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Tasks;

namespace H2l.Application.Tasks.Queries;

public sealed class GetTaskDetailQueryHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;

    public GetTaskDetailQueryHandler(
        ICurrentUserContext currentUserContext,
        IHumanTaskRepository humanTaskRepository,
        ITaskAccessEvaluator taskAccessEvaluator)
    {
        _currentUserContext = currentUserContext;
        _humanTaskRepository = humanTaskRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
    }

    public async Task<Result<TaskDetailResponse>> Handle(
        GetTaskDetailQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        var detail = await _humanTaskRepository.GetDetailByIdAsync(query.TaskId, cancellationToken);
        if (detail is null)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.NotFound("TASK_NOT_FOUND", "Human task was not found."));
        }

        var userId = _currentUserContext.UserId.Value;
        var hasAccess = await _taskAccessEvaluator.CanUserAccessTaskAsync(
            userId,
            detail.Task,
            detail.FormDefinitionId,
            cancellationToken);

        if (!hasAccess)
        {
            return Result.Failure<TaskDetailResponse>(
                Error.Forbidden("TASK_ACCESS_DENIED", "You do not have permission to access this task."));
        }

        return Result.Success(TaskDetailResponseMapper.ToDetailResponse(detail.Task, detail.FormVersion, userId));
    }
}
