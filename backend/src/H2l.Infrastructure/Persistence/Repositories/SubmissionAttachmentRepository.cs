using H2l.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class SubmissionAttachmentRepository : ISubmissionAttachmentRepository
{
    private readonly H2lDbContext _dbContext;

    public SubmissionAttachmentRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SubmissionAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.SubmissionAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SubmissionAttachment>> ListByIdsForTaskForUpdateAsync(
        Guid humanTaskId,
        IReadOnlyCollection<Guid> attachmentIds,
        CancellationToken cancellationToken = default)
    {
        if (attachmentIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.SubmissionAttachments
            .Where(a => a.HumanTaskId == humanTaskId && attachmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SubmissionAttachment attachment, CancellationToken cancellationToken = default)
        => await _dbContext.SubmissionAttachments.AddAsync(attachment, cancellationToken);
}
