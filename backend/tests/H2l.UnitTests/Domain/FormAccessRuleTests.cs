using H2l.Domain.Common;
using H2l.Domain.Workflows;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class FormAccessRuleTests
{
    [Fact]
    public void Create_WithGroupOnly_DerivesWholeGroupScope()
    {
        var result = FormAccessRule.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null);

        Assert.True(result.IsSuccess);
        Assert.Equal(FormAccessScope.WholeGroup, result.Value.AccessScope);
    }

    [Fact]
    public void Create_WithGroupAndUser_DerivesGroupUserSubsetScope()
    {
        var result = FormAccessRule.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());

        Assert.True(result.IsSuccess);
        Assert.Equal(FormAccessScope.GroupUserSubset, result.Value.AccessScope);
    }

    [Fact]
    public void Create_WithUserOnly_DerivesDirectUserScope()
    {
        var result = FormAccessRule.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            Guid.CreateVersion7());

        Assert.True(result.IsSuccess);
        Assert.Equal(FormAccessScope.DirectUser, result.Value.AccessScope);
    }

    [Fact]
    public void Create_WithBothGroupAndUserNull_ReturnsValidationFailure()
    {
        var result = FormAccessRule.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("FORM_ACCESS_RULE_TARGET_REQUIRED", result.Error.Code);
    }
}
