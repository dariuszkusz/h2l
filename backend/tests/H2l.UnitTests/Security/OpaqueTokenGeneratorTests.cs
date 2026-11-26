using H2l.Infrastructure.Security;
using Xunit;

namespace H2l.UnitTests.Security;

public sealed class OpaqueTokenGeneratorTests
{
    private readonly OpaqueTokenGenerator _generator = new();

    [Fact]
    public void GenerateTokenAndHash_ProducesUniqueUrlSafeTokensAndDeterministicSha256Hex()
    {
        var token1 = _generator.GenerateToken();
        var token2 = _generator.GenerateToken();

        Assert.NotEqual(token1, token2);
        Assert.DoesNotContain('+', token1);
        Assert.DoesNotContain('/', token1);
        Assert.DoesNotContain('=', token1);

        var hash1 = _generator.ComputeSha256Hash(token1);
        var hash1Repeat = _generator.ComputeSha256Hash(token1);

        Assert.Equal(64, hash1.Length);
        Assert.Equal(hash1, hash1Repeat);
    }
}
