using H2l.Application.Connectors.Contracts;
using H2l.Domain.Connectors;

namespace H2l.Infrastructure.Connectors;

public sealed class WorkflowConnectorRegistry : IWorkflowConnectorRegistry
{
    private readonly IReadOnlyDictionary<ConnectorType, IWorkflowConnector> _connectors;

    public WorkflowConnectorRegistry(IEnumerable<IWorkflowConnector> connectors)
    {
        _connectors = connectors.ToDictionary(c => c.ConnectorType);
    }

    public IWorkflowConnector GetConnector(ConnectorType connectorType)
    {
        if (_connectors.TryGetValue(connectorType, out var connector))
        {
            return connector;
        }

        throw new InvalidOperationException($"No workflow connector registered for type '{connectorType}'.");
    }

    public IPullWorkflowConnector? TryGetPullConnector(ConnectorType connectorType)
        => _connectors.TryGetValue(connectorType, out var connector)
            ? connector as IPullWorkflowConnector
            : null;
}
