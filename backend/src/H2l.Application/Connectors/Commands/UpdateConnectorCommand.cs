using H2l.Application.Connectors.DTOs;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Commands;

public sealed record UpdateConnectorCommand(
    Guid ConnectorId,
    string Name,
    ConnectorType ConnectorType,
    string BaseUrl,
    ConnectorAuthType AuthType,
    bool IsActive,
    ConnectorCredentialsRequest? Credentials,
    string? CustomCaCertificatePem,
    bool ClearCustomCaCertificate,
    string? CallbackUrl);
