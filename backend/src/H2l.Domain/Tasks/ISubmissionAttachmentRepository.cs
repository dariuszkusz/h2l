namespace H2l.Domain.Tasks;

public interface ISubmissionAttachmentRepository
{
    Task<SubmissionAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubmissionAttachment>> ListByIdsForTaskForUpdateAsync(
        Guid humanTaskId,
        IReadOnlyCollection<Guid> attachmentIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(SubmissionAttachment attachment, CancellationToken cancellationToken = default);
}
