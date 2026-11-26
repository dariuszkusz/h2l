namespace H2l.Domain.Workflows;

public interface IFormDefinitionRepository
{
    Task<FormDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FormDefinition?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FormDefinition>> ListByProcessIdAsync(Guid workflowProcessId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByStepKeyAsync(Guid workflowProcessId, string stepKey, CancellationToken cancellationToken = default);

    Task AddAsync(FormDefinition formDefinition, CancellationToken cancellationToken = default);
}
