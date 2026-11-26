using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class UserActivationTokenTests
{
    [Fact]
    public void Create_AndMarkUsed_TracksSingleUseAndExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var tokenResult = UserActivationToken.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "sha256-hash-value",
            now.AddHours(24),
            now);

        Assert.True(tokenResult.IsSuccess);
        var token = tokenResult.Value;
        Assert.True(token.IsValidAt(now.AddHours(1)));

        var firstUse = token.MarkUsed(now.AddHours(1));
        Assert.True(firstUse.IsSuccess);
        Assert.False(token.IsValidAt(now.AddHours(2)));

        var secondUse = token.MarkUsed(now.AddHours(2));
        Assert.True(secondUse.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondUse.Error.Type);
        Assert.Equal("TOKEN_ALREADY_USED", secondUse.Error.Code);
    }

    [Fact]
    public void MarkUsed_AfterExpiration_ReturnsValidationFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var token = UserActivationToken.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "sha256-hash-value",
            now.AddHours(1),
            now).Value;

        var result = token.MarkUsed(now.AddHours(2));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("TOKEN_EXPIRED", result.Error.Code);
    }
}
