using H2l.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class FormSubmissionRepository : IFormSubmissionRepository
{
    private readonly H2lDbContext _dbContext;

    public FormSubmissionRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FormSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<FormSubmission?> GetByIdIgnoringTenantFilterAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FormSubmissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<FormSubmission?> GetByTaskAndIdempotencyKeyAsync(
        Guid humanTaskId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
        => _dbContext.FormSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.HumanTaskId == humanTaskId && s.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public Task<FormSubmission?> GetByHumanTaskIdAsync(
        Guid humanTaskId,
        CancellationToken cancellationToken = default)
        => _dbContext.FormSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.HumanTaskId == humanTaskId, cancellationToken);

    public async Task<IReadOnlyList<FormSubmission>> ListExpiredForRetentionIgnoringTenantFilterAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
        => await _dbContext.FormSubmissions
            .IgnoreQueryFilters()
            .Where(s => s.AnonymizedAt == null && s.RetentionUntil != null && s.RetentionUntil <= utcNow)
            .OrderBy(s => s.RetentionUntil)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FormSubmission submission, CancellationToken cancellationToken = default)
        => await _dbContext.FormSubmissions.AddAsync(submission, cancellationToken);
}
