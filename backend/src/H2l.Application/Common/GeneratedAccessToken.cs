namespace H2l.Application.Common;

public sealed record GeneratedAccessToken(
    string AccessToken,
    int ExpiresInSeconds,
    int RefreshTokenLifetimeDays);
