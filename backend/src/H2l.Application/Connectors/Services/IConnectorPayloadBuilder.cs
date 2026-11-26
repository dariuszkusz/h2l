using H2l.Application.Connectors.Contracts;

namespace H2l.Application.Connectors.Services;

public interface IConnectorPayloadBuilder
{
    string BuildWebhookCompletionBody(
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload);

    string BuildWebhookCancellationBody(
        ConnectorTaskContext taskContext,
        string status,
        string reason);

    Dictionary<string, object?> BuildCamundaCompletionVariables(
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload);
}
