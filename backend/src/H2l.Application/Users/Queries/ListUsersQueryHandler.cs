using H2l.Application.Users.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Users.Queries;

public sealed class ListUsersQueryHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;

    public ListUsersQueryHandler(
        IUserRepository userRepository,
        IGroupRepository groupRepository)
    {
        _userRepository = userRepository;
        _groupRepository = groupRepository;
    }

    public async Task<Result<IReadOnlyList<UserSummaryResponse>>> Handle(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToArray();
        var groupMap = await _groupRepository.ListGroupIdsByUserIdsAsync(userIds, cancellationToken);

        var items = users
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.Email,
                u.ExternalId,
                u.SystemRole,
                u.PreferredLanguage,
                u.PreferredTheme,
                u.IsActive,
                groupMap.TryGetValue(u.Id, out var groupIds) ? groupIds : Array.Empty<Guid>(),
                u.CreatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<UserSummaryResponse>>(items);
    }
}
