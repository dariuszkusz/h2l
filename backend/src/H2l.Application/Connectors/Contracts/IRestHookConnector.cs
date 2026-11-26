using H2l.Domain.Common;

namespace H2l.Application.Connectors.Contracts;

public interface IRestHookConnector : IWorkflowConnector
{
    Task<Result<string>> SubscribeAsync(
        ConnectorRuntimeConfig config,
        string targetWebhookUrl,
        CancellationToken cancellationToken = default);

    Task<Result> UnsubscribeAsync(
        ConnectorRuntimeConfig config,
        string subscriptionId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyDictionary<string, object?>>> GetSamplePayloadAsync(
        Guid formVersionId,
        CancellationToken cancellationToken = default);
}
