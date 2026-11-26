using H2l.Infrastructure.Security;
using Xunit;

namespace H2l.UnitTests.Security;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void HashAndVerify_WithMatchingPassword_ReturnsTrueAndUsesUniqueSalts()
    {
        var hash1 = _hasher.Hash("StrongPassword!123");
        var hash2 = _hasher.Hash("StrongPassword!123");

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.Verify("StrongPassword!123", hash1));
        Assert.True(_hasher.Verify("StrongPassword!123", hash2));
        Assert.False(_hasher.Verify("WrongPassword!123", hash1));
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("StrongPassword!123", "not-a-valid-hash"));
    }
}
