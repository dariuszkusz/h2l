namespace H2l.Application.Auth.DTOs;

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    UserProfileResponse User);
