using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Contracts;

public interface IWorkflowConnector
{
    ConnectorType ConnectorType { get; }

    Task<ConnectorTestResult> TestConnectionAsync(
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken = default);

    Task<ConnectorDeliveryResult> CompleteTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload,
        CancellationToken cancellationToken = default);

    Task<ConnectorDeliveryResult> CancelTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        string status,
        string reason,
        CancellationToken cancellationToken = default);
}
