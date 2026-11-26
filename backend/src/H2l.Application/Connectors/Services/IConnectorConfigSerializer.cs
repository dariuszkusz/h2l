using H2l.Application.Connectors.Contracts;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Services;

public interface IConnectorConfigSerializer
{
    ConnectorRuntimeConfig DeserializeRuntimeConfig(WorkflowConnector connector);

    (string? EncryptedConfig, bool HasCredentials, bool HasCustomCaCertificate) SerializeAndEncrypt(
        Guid tenantId,
        ConnectorCredentialsData? credentials,
        string? customCaCertificatePem);

    (string? EncryptedConfig, bool HasCredentials, bool HasCustomCaCertificate) MergeAndEncrypt(
        WorkflowConnector existing,
        ConnectorCredentialsData? incomingCredentials,
        string? incomingCustomCaPem,
        bool clearCustomCaCertificate);
}
