using H2l.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly H2lDbContext _dbContext;

    public OutboxMessageRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        => await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
}
