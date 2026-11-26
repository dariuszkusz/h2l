using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.DTOs;

public sealed record ConnectorSummaryResponse(
    Guid Id,
    string Name,
    ConnectorType ConnectorType,
    string BaseUrl,
    ConnectorAuthType AuthType,
    bool HasCredentials,
    bool HasCustomCaCertificate,
    string? CallbackUrl,
    bool IsActive,
    DateTimeOffset CreatedAt);
