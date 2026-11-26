using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly H2lDbContext _dbContext;

    public GroupRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<Group?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return _dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Groups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Group>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Groups
            .AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return _dbContext.Groups
            .AsNoTracking()
            .AnyAsync(g => g.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
        => await _dbContext.Groups.AddAsync(group, cancellationToken);

    public async Task<IReadOnlyList<User>> ListMembersAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
        => await _dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => ug.GroupId == groupId)
            .Join(
                _dbContext.Users.AsNoTracking(),
                ug => ug.UserId,
                u => u.Id,
                (_, u) => u)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

    public Task<UserGroup?> GetMembershipAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => _dbContext.UserGroups
            .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId, cancellationToken);

    public async Task AddMembershipAsync(UserGroup membership, CancellationToken cancellationToken = default)
        => await _dbContext.UserGroups.AddAsync(membership, cancellationToken);

    public void RemoveMembership(UserGroup membership)
        => _dbContext.UserGroups.Remove(membership);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> ListGroupIdsByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<Guid>>();
        }

        var rows = await _dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => userIds.Contains(ug.UserId))
            .Select(ug => new { ug.UserId, ug.GroupId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Guid>)g.Select(x => x.GroupId).OrderBy(id => id).ToList());
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountMembersByGroupIdsAsync(
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await _dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => groupIds.Contains(ug.GroupId))
            .GroupBy(ug => ug.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.GroupId, r => r.Count);
    }

    public async Task<IReadOnlySet<(Guid GroupId, Guid UserId)>> GetExistingMembershipsAsync(
        IReadOnlyCollection<Guid> groupIds,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0 || userIds.Count == 0)
        {
            return new HashSet<(Guid GroupId, Guid UserId)>();
        }

        var rows = await _dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => groupIds.Contains(ug.GroupId) && userIds.Contains(ug.UserId))
            .Select(ug => new { ug.GroupId, ug.UserId })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.GroupId, r.UserId)).ToHashSet();
    }
}
