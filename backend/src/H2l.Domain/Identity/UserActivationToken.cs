using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class UserActivationToken : Entity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private UserActivationToken()
    {
    }

    private UserActivationToken(
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

    public static Result<UserActivationToken> Create(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<UserActivationToken>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<UserActivationToken>(Error.Validation("USER_ID_REQUIRED", "User identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Result.Failure<UserActivationToken>(Error.Validation("TOKEN_HASH_REQUIRED", "Activation token hash is required."));
        }

        var normalizedHash = tokenHash.Trim();
        if (normalizedHash.Length > 128)
        {
            return Result.Failure<UserActivationToken>(Error.Validation("TOKEN_HASH_TOO_LONG", "Activation token hash cannot exceed 128 characters."));
        }

        var effectiveCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        if (expiresAt <= effectiveCreatedAt)
        {
            return Result.Failure<UserActivationToken>(Error.Validation("TOKEN_EXPIRY_INVALID", "Activation token expiration must be in the future."));
        }

        var token = new UserActivationToken(
            Guid.CreateVersion7(),
            tenantId,
            userId,
            normalizedHash,
            expiresAt,
            effectiveCreatedAt);

        return Result.Success(token);
    }

    public bool IsValidAt(DateTimeOffset timestamp) => UsedAt is null && ExpiresAt > timestamp;

    public Result MarkUsed(DateTimeOffset usedAt)
    {
        if (UsedAt is not null)
        {
            return Result.Failure(Error.Conflict("TOKEN_ALREADY_USED", "Activation token has already been used."));
        }

        if (ExpiresAt <= usedAt)
        {
            return Result.Failure(Error.Validation("TOKEN_EXPIRED", "Activation token has expired."));
        }

        UsedAt = usedAt;
        return Result.Success();
    }
}
