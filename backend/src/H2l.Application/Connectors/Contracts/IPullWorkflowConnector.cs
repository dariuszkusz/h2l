namespace H2l.Application.Connectors.Contracts;

public interface IPullWorkflowConnector : IWorkflowConnector
{
    Task<ConnectorDeliveryResult> ExtendLockAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        TimeSpan newTimeout,
        CancellationToken cancellationToken = default);
}
