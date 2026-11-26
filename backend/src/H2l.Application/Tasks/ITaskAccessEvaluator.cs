using H2l.Domain.Tasks;

namespace H2l.Application.Tasks;

public interface ITaskAccessEvaluator
{
    Task<bool> CanUserAccessTaskAsync(
        Guid userId,
        HumanTask task,
        Guid formDefinitionId,
        CancellationToken cancellationToken = default);

    Task<bool> CanUserAccessFormAsync(
        Guid userId,
        Guid formDefinitionId,
        CancellationToken cancellationToken = default);
}
