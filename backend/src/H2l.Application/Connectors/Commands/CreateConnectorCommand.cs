using H2l.Application.Connectors.DTOs;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Commands;

public sealed record CreateConnectorCommand(
    string Name,
    ConnectorType ConnectorType,
    string BaseUrl,
    ConnectorAuthType AuthType,
    ConnectorCredentialsRequest? Credentials,
    string? CustomCaCertificatePem,
    string? CallbackUrl,
    bool IsActive);
