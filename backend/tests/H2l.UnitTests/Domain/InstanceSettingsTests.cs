using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class InstanceSettingsTests
{
    [Fact]
    public void Create_WithValidTenantId_DefaultsPaletteToSlateAndGeneratesUuidV7()
    {
        var tenantId = Guid.CreateVersion7();

        var result = InstanceSettings.Create(tenantId);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, result.Value.TenantId);
        Assert.Equal(7, result.Value.Id.Version);
        Assert.Equal(7, result.Value.InstanceId.Version);
        Assert.NotEqual(result.Value.Id, result.Value.InstanceId);
        Assert.Equal("slate", result.Value.SelectedPalette);
        Assert.Equal("en", result.Value.DefaultLanguage);
        Assert.Equal(90, result.Value.DefaultRetentionDays);
    }

    [Theory]
    [InlineData("slate", "slate")]
    [InlineData("OCEAN", "ocean")]
    [InlineData("  Forest ", "forest")]
    public void Create_WithSupportedPalette_NormalizesToLowercase(string inputPalette, string expectedPalette)
    {
        var result = InstanceSettings.Create(Guid.CreateVersion7(), inputPalette, "PL", 180);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPalette, result.Value.SelectedPalette);
        Assert.Equal("pl", result.Value.DefaultLanguage);
        Assert.Equal(180, result.Value.DefaultRetentionDays);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("neon")]
    [InlineData("custom")]
    public void Create_WithUnsupportedPalette_ReturnsValidationFailure(string invalidPalette)
    {
        var result = InstanceSettings.Create(Guid.CreateVersion7(), invalidPalette, "en");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("PALETTE_UNSUPPORTED", result.Error.Code);
    }

    [Fact]
    public void Create_WithUnsupportedLanguage_ReturnsValidationFailure()
    {
        var result = InstanceSettings.Create(Guid.CreateVersion7(), "slate", "fr");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("LANGUAGE_UNSUPPORTED", result.Error.Code);
    }

    [Fact]
    public void UpdateConfiguration_WithNonPositiveRetentionDays_ReturnsValidationFailure()
    {
        var settings = InstanceSettings.Create(Guid.CreateVersion7()).Value;

        var updateResult = settings.UpdateConfiguration("ocean", "de", 0, null);

        Assert.True(updateResult.IsFailure);
        Assert.Equal("RETENTION_DAYS_INVALID", updateResult.Error.Code);
    }

    [Fact]
    public void UpdateConfiguration_WithUnsupportedPalette_ReturnsValidationFailure()
    {
        var settings = InstanceSettings.Create(Guid.CreateVersion7()).Value;

        var updateResult = settings.UpdateConfiguration("default", "de", 90, null);

        Assert.True(updateResult.IsFailure);
        Assert.Equal(ErrorType.Validation, updateResult.Error.Type);
        Assert.Equal("PALETTE_UNSUPPORTED", updateResult.Error.Code);
    }
}
