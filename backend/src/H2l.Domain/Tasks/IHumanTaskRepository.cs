namespace H2l.Domain.Tasks;

public interface IHumanTaskRepository
{
    Task<HumanTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HumanTask?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HumanTaskDetailRecord?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedHumanTasksResult> ListForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> userGroupIds,
        HumanTaskStatus? status,
        TaskListScope scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(HumanTask task, CancellationToken cancellationToken = default);
}
