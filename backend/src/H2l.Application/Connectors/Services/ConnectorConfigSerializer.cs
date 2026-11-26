using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Services;

public sealed class ConnectorConfigSerializer : IConnectorConfigSerializer
{
    private readonly IAesGcmEncryptionService _encryptionService;

    public ConnectorConfigSerializer(IAesGcmEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public ConnectorRuntimeConfig DeserializeRuntimeConfig(WorkflowConnector connector)
    {
        ConnectorSecretEnvelope? envelope = null;
        if (!string.IsNullOrWhiteSpace(connector.EncryptedConfig))
        {
            var decryptedJson = _encryptionService.Decrypt(
                connector.EncryptedConfig,
                BuildAad(connector.TenantId));
            envelope = JsonSerializer.Deserialize<ConnectorSecretEnvelope>(decryptedJson);
        }

        return new ConnectorRuntimeConfig(
            connector.Id,
            connector.TenantId,
            connector.Name,
            connector.ConnectorType,
            connector.AuthType,
            connector.BaseUrl,
            connector.CallbackUrl,
            envelope?.Credentials,
            envelope?.CustomCaCertificatePem);
    }

    public (string? EncryptedConfig, bool HasCredentials, bool HasCustomCaCertificate) SerializeAndEncrypt(
        Guid tenantId,
        ConnectorCredentialsData? credentials,
        string? customCaCertificatePem)
    {
        var normalizedPem = string.IsNullOrWhiteSpace(customCaCertificatePem)
            ? null
            : customCaCertificatePem.Trim();
        var hasCredentials = HasAnyCredentialValue(credentials);
        var hasCustomCa = normalizedPem is not null;

        if (!hasCredentials && !hasCustomCa)
        {
            return (null, false, false);
        }

        var envelope = new ConnectorSecretEnvelope(
            hasCredentials ? credentials : null,
            normalizedPem);

        var json = JsonSerializer.Serialize(envelope);
        var encrypted = _encryptionService.Encrypt(json, BuildAad(tenantId));
        return (encrypted, hasCredentials, hasCustomCa);
    }

    public (string? EncryptedConfig, bool HasCredentials, bool HasCustomCaCertificate) MergeAndEncrypt(
        WorkflowConnector existing,
        ConnectorCredentialsData? incomingCredentials,
        string? incomingCustomCaPem,
        bool clearCustomCaCertificate)
    {
        var current = DeserializeRuntimeConfig(existing);

        var effectiveCredentials = incomingCredentials is not null
            ? MergeCredentials(current.Credentials, incomingCredentials)
            : current.Credentials;

        string? effectiveCaPem;
        if (clearCustomCaCertificate)
        {
            effectiveCaPem = null;
        }
        else if (!string.IsNullOrWhiteSpace(incomingCustomCaPem))
        {
            effectiveCaPem = incomingCustomCaPem.Trim();
        }
        else
        {
            effectiveCaPem = current.CustomCaCertificatePem;
        }

        return SerializeAndEncrypt(existing.TenantId, effectiveCredentials, effectiveCaPem);
    }

    private static ConnectorCredentialsData MergeCredentials(
        ConnectorCredentialsData? existing,
        ConnectorCredentialsData incoming)
    {
        if (existing is null)
        {
            return incoming;
        }

        return new ConnectorCredentialsData(
            Username: incoming.Username ?? existing.Username,
            Password: incoming.Password ?? existing.Password,
            BearerToken: incoming.BearerToken ?? existing.BearerToken,
            ApiKeyHeaderName: incoming.ApiKeyHeaderName ?? existing.ApiKeyHeaderName,
            ApiKey: incoming.ApiKey ?? existing.ApiKey,
            HmacSecret: incoming.HmacSecret ?? existing.HmacSecret,
            ClientId: incoming.ClientId ?? existing.ClientId,
            ClientSecret: incoming.ClientSecret ?? existing.ClientSecret,
            TokenEndpoint: incoming.TokenEndpoint ?? existing.TokenEndpoint,
            Audience: incoming.Audience ?? existing.Audience,
            Scope: incoming.Scope ?? existing.Scope);
    }

    private static bool HasAnyCredentialValue(ConnectorCredentialsData? credentials)
    {
        if (credentials is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(credentials.Username)
            || !string.IsNullOrWhiteSpace(credentials.Password)
            || !string.IsNullOrWhiteSpace(credentials.BearerToken)
            || !string.IsNullOrWhiteSpace(credentials.ApiKey)
            || !string.IsNullOrWhiteSpace(credentials.HmacSecret)
            || !string.IsNullOrWhiteSpace(credentials.ClientId)
            || !string.IsNullOrWhiteSpace(credentials.ClientSecret)
            || !string.IsNullOrWhiteSpace(credentials.TokenEndpoint);
    }

    private static string BuildAad(Guid tenantId)
        => $"connector:{tenantId:N}";
}
