using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly H2lDbContext _dbContext;

    public UserRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAdminExistsAsync(CancellationToken cancellationToken = default)
        => _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.SystemRole == SystemRole.Admin && u.IsActive, cancellationToken);

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
        => _dbContext.Users
            .AsNoTracking()
            .CountAsync(u => u.SystemRole == SystemRole.Admin && u.IsActive, cancellationToken);

    public Task<int> CountActiveEndUsersAsync(CancellationToken cancellationToken = default)
        => _dbContext.Users
            .AsNoTracking()
            .CountAsync(u => u.SystemRole == SystemRole.EndUser && u.IsActive, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _dbContext.Users.AddAsync(user, cancellationToken);
}
