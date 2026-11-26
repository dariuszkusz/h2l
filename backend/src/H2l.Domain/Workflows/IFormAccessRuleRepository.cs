namespace H2l.Domain.Workflows;

public interface IFormAccessRuleRepository
{
    Task<IReadOnlyList<FormAccessRule>> ListByFormDefinitionIdAsync(Guid formDefinitionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FormAccessRule>> ListByFormDefinitionIdForUpdateAsync(Guid formDefinitionId, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<FormAccessRule> rules, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<FormAccessRule> rules);
}
