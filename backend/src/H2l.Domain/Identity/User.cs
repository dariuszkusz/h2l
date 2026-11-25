using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class User : AggregateRoot, ITenantScoped, ISoftDeletable
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "de",
        "pl"
    };

    private static readonly HashSet<string> SupportedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light",
        "dark",
        "high-contrast"
    };

    public Guid TenantId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string? ExternalId { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public SystemRole SystemRole { get; private set; }

    public string PreferredLanguage { get; private set; } = "en";

    public string PreferredTheme { get; private set; } = "light";

    public bool IsActive { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    private User(
        Guid id,
        Guid tenantId,
        string email,
        string passwordHash,
        SystemRole systemRole,
        string preferredLanguage,
        string preferredTheme,
        string? externalId,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        SystemRole = systemRole;
        PreferredLanguage = preferredLanguage;
        PreferredTheme = preferredTheme;
        ExternalId = externalId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static Result<User> Create(
        Guid tenantId,
        string email,
        string passwordHash,
        SystemRole systemRole,
        string preferredLanguage = "en",
        string preferredTheme = "light",
        string? externalId = null,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<User>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result.Failure<User>(Error.Validation("USER_EMAIL_INVALID", "A valid user email is required."));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure<User>(Error.Validation("USER_PASSWORD_HASH_REQUIRED", "Password hash is required."));
        }

        if (!SupportedLanguages.Contains(preferredLanguage))
        {
            return Result.Failure<User>(Error.Validation("LANGUAGE_UNSUPPORTED", "Preferred language must be one of: en, de, pl."));
        }

        if (!SupportedThemes.Contains(preferredTheme))
        {
            return Result.Failure<User>(Error.Validation("THEME_UNSUPPORTED", "Preferred theme must be one of: light, dark, high-contrast."));
        }

        var user = new User(
            Guid.CreateVersion7(),
            tenantId,
            email.Trim().ToLowerInvariant(),
            passwordHash,
            systemRole,
            preferredLanguage.Trim().ToLowerInvariant(),
            preferredTheme.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim(),
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(user);
    }

    public Result UpdatePreferences(string? preferredLanguage, string? preferredTheme)
    {
        if (preferredLanguage is not null && !SupportedLanguages.Contains(preferredLanguage))
        {
            return Result.Failure(Error.Validation("LANGUAGE_UNSUPPORTED", "Preferred language must be one of: en, de, pl."));
        }

        if (preferredTheme is not null && !SupportedThemes.Contains(preferredTheme))
        {
            return Result.Failure(Error.Validation("THEME_UNSUPPORTED", "Preferred theme must be one of: light, dark, high-contrast."));
        }

        if (preferredLanguage is not null)
        {
            PreferredLanguage = preferredLanguage.Trim().ToLowerInvariant();
        }

        if (preferredTheme is not null)
        {
            PreferredTheme = preferredTheme.Trim().ToLowerInvariant();
        }

        return Result.Success();
    }

    public void SoftDelete(DateTimeOffset? deletedAt = null)
    {
        DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
        IsActive = false;
    }
}
