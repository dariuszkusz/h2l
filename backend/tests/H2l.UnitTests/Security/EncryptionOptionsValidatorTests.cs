using System.Security.Cryptography;
using H2l.Infrastructure.Options;
using Xunit;

namespace H2l.UnitTests.Security;

public sealed class EncryptionOptionsValidatorTests
{
    [Fact]
    public void Validate_WithValid32ByteBase64Key_Succeeds()
    {
        var validator = new EncryptionOptionsValidator();
        var options = new EncryptionOptions
        {
            KeyId = "v1",
            AesGcmKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("v1", "")]
    [InlineData("v1", "REPLACE_WITH_BASE64_ENCODED_32_BYTE_AES_GCM_KEY")]
    [InlineData("v1", "not-valid-base64!!!")]
    [InlineData("v1", "AAAAAAAAAAAAAAAAAAAAAA==")] // 16 bytes instead of 32
    [InlineData("v1:bad", "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=")]
    public void Validate_WithInvalidOptions_Fails(string keyId, string aesGcmKey)
    {
        var validator = new EncryptionOptionsValidator();
        var options = new EncryptionOptions
        {
            KeyId = keyId,
            AesGcmKey = aesGcmKey
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }
}
