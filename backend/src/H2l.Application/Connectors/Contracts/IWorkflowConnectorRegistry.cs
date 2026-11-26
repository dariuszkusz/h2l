using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Contracts;

public interface IWorkflowConnectorRegistry
{
    IWorkflowConnector GetConnector(ConnectorType connectorType);

    IPullWorkflowConnector? TryGetPullConnector(ConnectorType connectorType);
}
