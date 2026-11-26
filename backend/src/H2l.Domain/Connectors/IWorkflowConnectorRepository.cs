namespace H2l.Domain.Connectors;

public interface IWorkflowConnectorRepository
{
    Task<WorkflowConnector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WorkflowConnector?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WorkflowConnector?> GetByIdIgnoringTenantFilterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowConnector>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowConnector>> ListActiveByTypeIgnoringTenantFilterAsync(
        ConnectorType connectorType,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeConnectorId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(WorkflowConnector connector, CancellationToken cancellationToken = default);
}
