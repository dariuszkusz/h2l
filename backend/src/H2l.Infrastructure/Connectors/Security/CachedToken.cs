namespace H2l.Infrastructure.Connectors.Security;

internal sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);
