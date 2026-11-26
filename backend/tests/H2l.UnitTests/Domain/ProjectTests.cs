using H2l.Domain.Common;
using H2l.Domain.Workflows;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class ProjectTests
{
    [Fact]
    public void Create_WithValidData_TrimsFieldsAndGeneratesUuidV7()
    {
        var result = Project.Create(Guid.CreateVersion7(), "  Procurement  ", "  Invoice approvals  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Id.Version);
        Assert.Equal("Procurement", result.Value.Name);
        Assert.Equal("Invoice approvals", result.Value.Description);
        Assert.Null(result.Value.DeletedAt);
    }

    [Fact]
    public void Create_WithBlankName_ReturnsValidationFailure()
    {
        var result = Project.Create(Guid.CreateVersion7(), "   ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("PROJECT_NAME_REQUIRED", result.Error.Code);
    }
}
