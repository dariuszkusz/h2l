namespace H2l.Domain.Workflows;

public interface IFormVersionRepository
{
    Task<FormVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FormVersion>> ListByFormDefinitionIdAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default);

    Task<int?> GetLatestVersionNumberAsync(
        Guid formDefinitionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(FormVersion formVersion, CancellationToken cancellationToken = default);
}
