using H2l.Application.Connectors.Contracts;

namespace H2l.Application.Connectors.Services;

public sealed record ConnectorSecretEnvelope(
    ConnectorCredentialsData? Credentials,
    string? CustomCaCertificatePem);
