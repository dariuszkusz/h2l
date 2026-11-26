namespace H2l.Application.Connectors.DTOs;

public sealed record ConnectorCredentialsRequest(
    string? Username = null,
    string? Password = null,
    string? BearerToken = null,
    string? ApiKeyHeaderName = null,
    string? ApiKey = null,
    string? HmacSecret = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? TokenEndpoint = null,
    string? Audience = null,
    string? Scope = null);
