using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class RefreshToken : Entity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static Result<RefreshToken> Create(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<RefreshToken>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<RefreshToken>(Error.Validation("USER_ID_REQUIRED", "User identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Result.Failure<RefreshToken>(Error.Validation("TOKEN_HASH_REQUIRED", "Refresh token hash is required."));
        }

        var normalizedHash = tokenHash.Trim();
        if (normalizedHash.Length > 128)
        {
            return Result.Failure<RefreshToken>(Error.Validation("TOKEN_HASH_TOO_LONG", "Refresh token hash cannot exceed 128 characters."));
        }

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        if (expiresAt <= effectiveCreatedAt)
        {
            return Result.Failure<RefreshToken>(Error.Validation("TOKEN_EXPIRY_INVALID", "Refresh token expiration must be in the future."));
        }

        var token = new RefreshToken(
            Guid.CreateVersion7(),
            tenantId,
            userId,
            normalizedHash,
            expiresAt,
            effectiveCreatedAt);

        return Result.Success(token);
    }

    public bool IsActiveAt(DateTimeOffset timestamp) => RevokedAt is null && ExpiresAt > timestamp;

    public Result Revoke(DateTimeOffset revokedAt, Guid? replacedByTokenId = null)
    {
        if (RevokedAt is not null)
        {
            return Result.Failure(Error.Conflict("TOKEN_ALREADY_REVOKED", "Refresh token has already been revoked."));
        }

        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
        return Result.Success();
    }
}
