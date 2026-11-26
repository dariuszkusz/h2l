using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class UserActivationTokenRepository : IUserActivationTokenRepository
{
    private readonly H2lDbContext _dbContext;

    public UserActivationTokenRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserActivationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => _dbContext.UserActivationTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<UserActivationToken?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => _dbContext.UserActivationTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(UserActivationToken token, CancellationToken cancellationToken = default)
        => await _dbContext.UserActivationTokens.AddAsync(token, cancellationToken);
}
