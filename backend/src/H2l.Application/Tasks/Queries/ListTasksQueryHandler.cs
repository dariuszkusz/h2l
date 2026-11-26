using H2l.Application.Common;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Tasks;

namespace H2l.Application.Tasks.Queries;

public sealed class ListTasksQueryHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IGroupRepository _groupRepository;
    private readonly IHumanTaskRepository _humanTaskRepository;

    public ListTasksQueryHandler(
        ICurrentUserContext currentUserContext,
        IGroupRepository groupRepository,
        IHumanTaskRepository humanTaskRepository)
    {
        _currentUserContext = currentUserContext;
        _groupRepository = groupRepository;
        _humanTaskRepository = humanTaskRepository;
    }

    public async Task<Result<TaskListResponse>> Handle(
        ListTasksQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<TaskListResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        var userId = _currentUserContext.UserId.Value;
        var groupsMap = await _groupRepository.ListGroupIdsByUserIdsAsync([userId], cancellationToken);
        var userGroupIds = groupsMap.TryGetValue(userId, out var groups)
            ? groups
            : Array.Empty<Guid>();

        var paged = await _humanTaskRepository.ListForUserAsync(
            userId,
            userGroupIds,
            query.Status,
            query.Scope,
            query.Page,
            query.PageSize,
            cancellationToken);

        var items = paged.Items
            .Select(item => new TaskSummaryResponse(
                item.Id,
                item.FormDefinitionId,
                item.FormVersionNumber,
                item.FormName,
                item.ProcessName,
                item.ProjectName,
                item.Status,
                item.AssignedUserId,
                item.ClaimedByUserId,
                CanClaim: item.AssignedUserId is null && item.ClaimedByUserId is null && item.Status == HumanTaskStatus.Pending,
                CanUnclaim: item.AssignedUserId is null && item.ClaimedByUserId == userId && item.Status == HumanTaskStatus.InProgress,
                item.ExpiresAt,
                item.CreatedAt))
            .ToList();

        var totalPages = paged.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(paged.TotalCount / (double)query.PageSize);

        return Result.Success(new TaskListResponse(
            items,
            query.Page,
            query.PageSize,
            paged.TotalCount,
            totalPages));
    }
}
