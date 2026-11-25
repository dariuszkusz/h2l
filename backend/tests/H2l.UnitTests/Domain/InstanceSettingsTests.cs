using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class InstanceSettingsTests
{
    [Fact]
    public void Create_WithValidTenantId_GeneratesUuidV7ForIdAndInstanceId()
    {
        var tenantId = Guid.CreateVersion7();

        var result = InstanceSettings.Create(tenantId, "slate", "PL", 180);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, result.Value.TenantId);
        Assert.Equal(7, result.Value.Id.Version);
        Assert.Equal(7, result.Value.InstanceId.Version);
        Assert.NotEqual(result.Value.Id, result.Value.InstanceId);
        Assert.Equal("pl", result.Value.DefaultLanguage);
        Assert.Equal(180, result.Value.DefaultRetentionDays);
    }

    [Fact]
    public void Create_WithUnsupportedLanguage_ReturnsValidationFailure()
    {
        var result = InstanceSettings.Create(Guid.CreateVersion7(), "default", "fr");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("LANGUAGE_UNSUPPORTED", result.Error.Code);
    }

    [Fact]
    public void UpdateConfiguration_WithNonPositiveRetentionDays_ReturnsValidationFailure()
    {
        var settings = InstanceSettings.Create(Guid.CreateVersion7()).Value;

        var updateResult = settings.UpdateConfiguration("default", "de", 0, null);

        Assert.True(updateResult.IsFailure);
        Assert.Equal("RETENTION_DAYS_INVALID", updateResult.Error.Code);
    }
}
