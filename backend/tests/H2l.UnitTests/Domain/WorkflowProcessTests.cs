using H2l.Domain.Common;
using H2l.Domain.Workflows;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class WorkflowProcessTests
{
    [Fact]
    public void Create_WithoutConnectorId_AllowsNullConnectorIdAndDefaultsToDraft()
    {
        var result = WorkflowProcess.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Expense Approval",
            "expense-approval");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ConnectorId);
        Assert.Equal(WorkflowProcessStatus.Draft, result.Value.Status);
    }

    [Fact]
    public void Create_WithEmptyConnectorId_ReturnsValidationFailure()
    {
        var result = WorkflowProcess.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Expense Approval",
            "expense-approval",
            Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("CONNECTOR_ID_INVALID", result.Error.Code);
    }

    [Fact]
    public void Activate_ArchivedProcess_ReturnsConflictFailure()
    {
        var process = WorkflowProcess.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Expense Approval",
            "expense-approval").Value;
        process.Archive();

        var result = process.Activate();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("PROCESS_ARCHIVED", result.Error.Code);
    }
}
