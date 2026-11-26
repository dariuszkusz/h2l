namespace H2l.Domain.Workflows;

public interface IWorkflowProcessRepository
{
    Task<WorkflowProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WorkflowProcess?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowProcess>> ListByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByExternalKeyAsync(Guid projectId, string externalProcessKey, CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(WorkflowProcess process, CancellationToken cancellationToken = default);
}
