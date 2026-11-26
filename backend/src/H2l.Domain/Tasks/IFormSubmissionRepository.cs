namespace H2l.Domain.Tasks;

public interface IFormSubmissionRepository
{
    Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FormSubmission?> GetByTaskAndIdempotencyKeyAsync(
        Guid humanTaskId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<FormSubmission?> GetByHumanTaskIdAsync(
        Guid humanTaskId,
        CancellationToken cancellationToken = default);

    Task AddAsync(FormSubmission submission, CancellationToken cancellationToken = default);
}
