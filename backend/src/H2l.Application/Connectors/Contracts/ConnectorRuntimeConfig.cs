using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Contracts;

public sealed record ConnectorRuntimeConfig(
    Guid ConnectorId,
    Guid TenantId,
    string Name,
    ConnectorType ConnectorType,
    ConnectorAuthType AuthType,
    string BaseUrl,
    string? CallbackUrl,
    ConnectorCredentialsData? Credentials,
    string? CustomCaCertificatePem);
