using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class UserGroupTests
{
    [Fact]
    public void Create_WithValidIds_ReturnsMembership()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var result = UserGroup.Create(tenantId, userId, groupId);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, result.Value.TenantId);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(groupId, result.Value.GroupId);
    }

    [Fact]
    public void Create_WithEmptyGroupId_ReturnsValidationFailure()
    {
        var result = UserGroup.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("GROUP_ID_REQUIRED", result.Error.Code);
    }
}
