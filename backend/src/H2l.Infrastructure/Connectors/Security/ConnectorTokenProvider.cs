using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Domain.Connectors;

namespace H2l.Infrastructure.Connectors.Security;

public sealed class ConnectorTokenProvider : ITokenProvider
{
    private readonly ConcurrentDictionary<string, CachedToken> _tokenCache = new(StringComparer.Ordinal);
    private readonly IConnectorHttpClientFactory _httpClientFactory;
    private readonly ISsrfGuard _ssrfGuard;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConnectorTokenProvider(
        IConnectorHttpClientFactory httpClientFactory,
        ISsrfGuard ssrfGuard,
        IDateTimeProvider dateTimeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _ssrfGuard = ssrfGuard;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string?> GetAuthorizationHeaderValueAsync(
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken = default)
    {
        switch (config.AuthType)
        {
            case ConnectorAuthType.None:
                return null;

            case ConnectorAuthType.Basic:
            {
                var username = config.Credentials?.Username ?? string.Empty;
                var password = config.Credentials?.Password ?? string.Empty;
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                return $"Basic {encoded}";
            }

            case ConnectorAuthType.Bearer:
            {
                var token = config.Credentials?.BearerToken;
                return string.IsNullOrWhiteSpace(token) ? null : $"Bearer {token.Trim()}";
            }

            case ConnectorAuthType.OidcClientCredentials:
            case ConnectorAuthType.OAuth2ClientCredentials:
            {
                var token = await FetchOrGetCachedOAuthTokenAsync(config, cancellationToken);
                return $"Bearer {token}";
            }

            default:
                return null;
        }
    }

    private async Task<string> FetchOrGetCachedOAuthTokenAsync(
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken)
    {
        var credentials = config.Credentials
            ?? throw new InvalidOperationException("OAuth2/OIDC credentials are not configured.");

        if (string.IsNullOrWhiteSpace(credentials.TokenEndpoint)
            || string.IsNullOrWhiteSpace(credentials.ClientId)
            || string.IsNullOrWhiteSpace(credentials.ClientSecret))
        {
            throw new InvalidOperationException("TokenEndpoint, ClientId, and ClientSecret are required for OAuth2/OIDC.");
        }

        var cacheKey = $"{config.ConnectorId:N}|{credentials.TokenEndpoint}|{credentials.ClientId}|{credentials.Audience}|{credentials.Scope}";
        var now = _dateTimeProvider.UtcNow;

        if (_tokenCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > now.AddSeconds(60))
        {
            return cached.AccessToken;
        }

        var ssrfResult = await _ssrfGuard.ValidateUrlAsync(credentials.TokenEndpoint, cancellationToken);
        if (ssrfResult.IsFailure)
        {
            throw new InvalidOperationException($"Token endpoint rejected by SSRF guard: {ssrfResult.Error.Code}");
        }

        using var httpClient = _httpClientFactory.CreateClient(config.CustomCaCertificatePem);
        var formParams = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = credentials.ClientId,
            ["client_secret"] = credentials.ClientSecret
        };

        if (!string.IsNullOrWhiteSpace(credentials.Audience))
        {
            formParams["audience"] = credentials.Audience;
        }

        if (!string.IsNullOrWhiteSpace(credentials.Scope))
        {
            formParams["scope"] = credentials.Scope;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ssrfResult.Value)
        {
            Content = new FormUrlEncodedContent(formParams)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OAuth2/OIDC token endpoint returned HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("access_token", out var tokenProp) ||
            string.IsNullOrWhiteSpace(tokenProp.GetString()))
        {
            throw new InvalidOperationException("OAuth2/OIDC token response did not contain access_token.");
        }

        var accessToken = tokenProp.GetString()!;
        var expiresInSeconds = 300;
        if (doc.RootElement.TryGetProperty("expires_in", out var expiresProp) &&
            expiresProp.TryGetInt32(out var parsedExpires) &&
            parsedExpires > 0)
        {
            expiresInSeconds = parsedExpires;
        }

        var expiresAt = now.AddSeconds(expiresInSeconds);
        _tokenCache[cacheKey] = new CachedToken(accessToken, expiresAt);
        return accessToken;
    }
}
