using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.DTOs;

public sealed record UpdateConnectorRequest(
    string Name,
    ConnectorType ConnectorType,
    string BaseUrl,
    ConnectorAuthType AuthType,
    bool IsActive,
    ConnectorCredentialsRequest? Credentials = null,
    string? CustomCaCertificatePem = null,
    bool ClearCustomCaCertificate = false,
    string? CallbackUrl = null);
