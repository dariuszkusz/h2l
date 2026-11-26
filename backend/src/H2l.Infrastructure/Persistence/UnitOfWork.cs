using H2l.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly H2lDbContext _dbContext;

    public UnitOfWork(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
