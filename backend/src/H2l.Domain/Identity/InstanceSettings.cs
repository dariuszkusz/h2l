using H2l.Domain.Common;

namespace H2l.Domain.Identity;

public sealed class InstanceSettings : AggregateRoot, ITenantScoped
{
    private static readonly HashSet<string> SupportedPalettes = new(StringComparer.OrdinalIgnoreCase)
    {
        "slate",
        "ocean",
        "forest"
    };

    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "de",
        "pl"
    };

    public Guid TenantId { get; private set; }

    public Guid InstanceId { get; private set; }

    public string SelectedPalette { get; private set; } = "slate";

    public string DefaultLanguage { get; private set; } = "en";

    public string? EncryptedLicenseKey { get; private set; }

    public string? EncryptedSmtpConfig { get; private set; }

    public int DefaultRetentionDays { get; private set; } = 90;

    public DateTimeOffset UpdatedAt { get; private set; }

    private InstanceSettings()
    {
    }

    private InstanceSettings(
        Guid id,
        Guid tenantId,
        Guid instanceId,
        string selectedPalette,
        string defaultLanguage,
        int defaultRetentionDays,
        DateTimeOffset updatedAt)
        : base(id)
    {
        TenantId = tenantId;
        InstanceId = instanceId;
        SelectedPalette = selectedPalette;
        DefaultLanguage = defaultLanguage;
        DefaultRetentionDays = defaultRetentionDays;
        UpdatedAt = updatedAt;
    }

    public static Result<InstanceSettings> Create(
        Guid tenantId,
        string selectedPalette = "slate",
        string defaultLanguage = "en",
        int defaultRetentionDays = 90,
        DateTimeOffset? updatedAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<InstanceSettings>(
                Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(selectedPalette))
        {
            return Result.Failure<InstanceSettings>(
                Error.Validation("PALETTE_REQUIRED", "Selected palette is required."));
        }

        var normalizedPalette = selectedPalette.Trim().ToLowerInvariant();
        if (!SupportedPalettes.Contains(normalizedPalette))
        {
            return Result.Failure<InstanceSettings>(
                Error.Validation("PALETTE_UNSUPPORTED", "Selected palette must be one of: slate, ocean, forest."));
        }

        if (!SupportedLanguages.Contains(defaultLanguage))
        {
            return Result.Failure<InstanceSettings>(
                Error.Validation("LANGUAGE_UNSUPPORTED", "Default language must be one of: en, de, pl."));
        }

        if (defaultRetentionDays <= 0)
        {
            return Result.Failure<InstanceSettings>(
                Error.Validation("RETENTION_DAYS_INVALID", "Default retention days must be greater than zero."));
        }

        var settings = new InstanceSettings(
            Guid.CreateVersion7(),
            tenantId,
            Guid.CreateVersion7(),
            normalizedPalette,
            defaultLanguage.Trim().ToLowerInvariant(),
            defaultRetentionDays,
            updatedAt ?? DateTimeOffset.UtcNow);

        return Result.Success(settings);
    }

    public Result UpdateConfiguration(
        string selectedPalette,
        string defaultLanguage,
        int defaultRetentionDays,
        string? encryptedSmtpConfig,
        DateTimeOffset? updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(selectedPalette))
        {
            return Result.Failure(Error.Validation("PALETTE_REQUIRED", "Selected palette is required."));
        }

        var normalizedPalette = selectedPalette.Trim().ToLowerInvariant();
        if (!SupportedPalettes.Contains(normalizedPalette))
        {
            return Result.Failure(
                Error.Validation("PALETTE_UNSUPPORTED", "Selected palette must be one of: slate, ocean, forest."));
        }

        if (!SupportedLanguages.Contains(defaultLanguage))
        {
            return Result.Failure(Error.Validation("LANGUAGE_UNSUPPORTED", "Default language must be one of: en, de, pl."));
        }

        if (defaultRetentionDays <= 0)
        {
            return Result.Failure(Error.Validation("RETENTION_DAYS_INVALID", "Default retention days must be greater than zero."));
        }

        SelectedPalette = normalizedPalette;
        DefaultLanguage = defaultLanguage.Trim().ToLowerInvariant();
        DefaultRetentionDays = defaultRetentionDays;
        EncryptedSmtpConfig = encryptedSmtpConfig;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public void SetEncryptedLicenseKey(string? encryptedLicenseKey, DateTimeOffset? updatedAt = null)
    {
        EncryptedLicenseKey = encryptedLicenseKey;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
    }
}
