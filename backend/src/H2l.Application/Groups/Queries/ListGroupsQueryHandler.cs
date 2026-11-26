using H2l.Application.Groups.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Groups.Queries;

public sealed class ListGroupsQueryHandler
{
    private readonly IGroupRepository _groupRepository;

    public ListGroupsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<Result<IReadOnlyList<GroupResponse>>> Handle(
        ListGroupsQuery query,
        CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.ListAsync(cancellationToken);
        var groupIds = groups.Select(g => g.Id).ToArray();
        var memberCounts = await _groupRepository.CountMembersByGroupIdsAsync(groupIds, cancellationToken);

        var items = groups
            .Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Code,
                memberCounts.TryGetValue(g.Id, out var count) ? count : 0,
                g.CreatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<GroupResponse>>(items);
    }
}
