using H2l.Application.Groups.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Groups.Queries;

public sealed class ListGroupMembersQueryHandler
{
    private readonly IGroupRepository _groupRepository;

    public ListGroupMembersQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<Result<IReadOnlyList<GroupMemberResponse>>> Handle(
        ListGroupMembersQuery query,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(query.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<IReadOnlyList<GroupMemberResponse>>(
                Error.NotFound("GROUP_NOT_FOUND", "Group was not found."));
        }

        var members = await _groupRepository.ListMembersAsync(query.GroupId, cancellationToken);
        var items = members
            .Select(u => new GroupMemberResponse(
                u.Id,
                u.Email,
                u.ExternalId,
                u.SystemRole,
                u.IsActive))
            .ToList();

        return Result.Success<IReadOnlyList<GroupMemberResponse>>(items);
    }
}
