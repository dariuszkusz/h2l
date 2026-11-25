using H2l.Domain.Common;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithValue_ExposesValueAndIsSuccessTrue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_AccessingValue_ThrowsInvalidOperationException()
    {
        var error = Error.Conflict("TASK_ALREADY_CLAIMED", "Task is already claimed.");
        var result = Result.Failure<int>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
