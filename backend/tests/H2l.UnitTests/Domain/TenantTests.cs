using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class TenantTests
{
    [Fact]
    public void Create_WithValidData_ReturnsTenantWithUuidV7AndNormalizedSlug()
    {
        var result = Tenant.Create("Acme Corp", "  Acme-Corp  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Corp", result.Value.Name);
        Assert.Equal("acme-corp", result.Value.Slug);
        Assert.True(result.Value.IsActive);
        Assert.Equal(7, result.Value.Id.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ReturnsValidationFailure(string name)
    {
        var result = Tenant.Create(name, "acme");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("TENANT_NAME_REQUIRED", result.Error.Code);
    }

    [Fact]
    public void DeactivateAndActivate_UpdatesIsActiveFlag()
    {
        var tenant = Tenant.Create("Acme", "acme").Value;

        tenant.Deactivate();
        Assert.False(tenant.IsActive);

        tenant.Activate();
        Assert.True(tenant.IsActive);
    }
}
