using H2l.Domain.Common;

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
}
