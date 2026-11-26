namespace H2l.Domain.Tasks;

public interface IHumanTaskRepository
{
    Task<HumanTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HumanTask?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HumanTask?> GetByIdIgnoringTenantFilterForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HumanTask?> GetByExternalTaskIdAsync(
        Guid connectorId,
        string externalTaskId,
        CancellationToken cancellationToken = default);

    Task<HumanTaskDetailRecord?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedHumanTasksResult> ListForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> userGroupIds,
        HumanTaskStatus? status,
        TaskListScope scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HumanTask>> ListExpiredOpenTasksIgnoringTenantFilterAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HumanTask>> ListActiveTasksByConnectorIgnoringTenantFilterAsync(
        Guid connectorId,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(HumanTask task, CancellationToken cancellationToken = default);
}
