using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class GroupTests
{
    [Fact]
    public void Create_WithValidData_NormalizesCodeAndGeneratesUuidV7()
    {
        var tenantId = Guid.CreateVersion7();

        var result = Group.Create(tenantId, "  Finance Approvers ", "  FIN_APPROVERS ");

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Id.Version);
        Assert.Equal("Finance Approvers", result.Value.Name);
        Assert.Equal("fin_approvers", result.Value.Code);
    }

    [Theory]
    [InlineData("", "code", "GROUP_NAME_REQUIRED")]
    [InlineData("Name", "", "GROUP_CODE_REQUIRED")]
    public void Create_WithMissingNameOrCode_ReturnsValidationFailure(string name, string code, string expectedError)
    {
        var result = Group.Create(Guid.CreateVersion7(), name, code);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedError, result.Error.Code);
    }
}
