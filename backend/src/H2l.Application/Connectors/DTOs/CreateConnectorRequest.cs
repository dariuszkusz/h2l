using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.DTOs;

public sealed record CreateConnectorRequest(
    string Name,
    ConnectorType ConnectorType,
    string BaseUrl,
    ConnectorAuthType AuthType,
    ConnectorCredentialsRequest? Credentials = null,
    string? CustomCaCertificatePem = null,
    string? CallbackUrl = null,
    bool IsActive = true);
