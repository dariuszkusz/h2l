using H2l.Domain.Common;
using H2l.Domain.Identity;
using Xunit;

namespace H2l.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Create_AndRevoke_TracksRotationAndRejectsDoubleRevoke()
    {
        var now = DateTimeOffset.UtcNow;
        var replacementId = Guid.CreateVersion7();
        var token = RefreshToken.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "refresh-sha256-hash",
            now.AddDays(7),
            now).Value;

        Assert.True(token.IsActiveAt(now.AddDays(1)));

        var revokeResult = token.Revoke(now.AddDays(1), replacementId);
        Assert.True(revokeResult.IsSuccess);
        Assert.False(token.IsActiveAt(now.AddDays(2)));
        Assert.Equal(replacementId, token.ReplacedByTokenId);

        var secondRevoke = token.Revoke(now.AddDays(2));
        Assert.True(secondRevoke.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondRevoke.Error.Type);
        Assert.Equal("TOKEN_ALREADY_REVOKED", secondRevoke.Error.Code);
    }
}
