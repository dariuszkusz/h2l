namespace H2l.Domain.Identity;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Group?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(Group group, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<UserGroup?> GetMembershipAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(UserGroup membership, CancellationToken cancellationToken = default);

    void RemoveMembership(UserGroup membership);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> ListGroupIdsByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> CountMembersByGroupIdsAsync(
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<(Guid GroupId, Guid UserId)>> GetExistingMembershipsAsync(
        IReadOnlyCollection<Guid> groupIds,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}
