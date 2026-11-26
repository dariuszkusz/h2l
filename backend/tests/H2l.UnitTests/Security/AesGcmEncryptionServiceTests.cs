using System.Security.Cryptography;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace H2l.UnitTests.Security;

public sealed class AesGcmEncryptionServiceTests
{
    private static AesGcmEncryptionService CreateService()
    {
        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var options = Microsoft.Extensions.Options.Options.Create(new EncryptionOptions
        {
            KeyId = "v1",
            AesGcmKey = Convert.ToBase64String(keyBytes)
        });

        return new AesGcmEncryptionService(options);
    }

    [Fact]
    public void EncryptAndDecrypt_WithUtf8AndAssociatedData_RoundTripsSuccessfully()
    {
        var service = CreateService();
        const string plaintext = "smtp-secret-hasło-żółć-Übung";
        const string aad = "tenant:default:smtp";

        var ciphertext = service.Encrypt(plaintext, aad);
        var decrypted = service.Decrypt(ciphertext, aad);

        Assert.Equal(plaintext, decrypted);
        Assert.StartsWith("v1:", ciphertext, StringComparison.Ordinal);
        Assert.Equal(4, ciphertext.Split(':').Length);
    }

    [Fact]
    public void Encrypt_SamePlaintextTwice_ProducesUniqueNoncesAndCiphertexts()
    {
        var service = CreateService();
        const string plaintext = "identical-secret-payload";

        var first = service.Encrypt(plaintext);
        var second = service.Encrypt(plaintext);

        Assert.NotEqual(first, second);
        Assert.NotEqual(first.Split(':')[1], second.Split(':')[1]);
        Assert.Equal(plaintext, service.Decrypt(first));
        Assert.Equal(plaintext, service.Decrypt(second));
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_ThrowsCryptographicException()
    {
        var service = CreateService();
        var ciphertext = service.Encrypt("sensitive-api-key", "aad-1");
        var parts = ciphertext.Split(':');

        var encryptedBytes = Convert.FromBase64String(parts[3]);
        encryptedBytes[0] ^= 0xFF;
        parts[3] = Convert.ToBase64String(encryptedBytes);
        var tampered = string.Join(':', parts);

        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(tampered, "aad-1"));
    }

    [Fact]
    public void Decrypt_WithWrongAssociatedData_ThrowsCryptographicException()
    {
        var service = CreateService();
        var ciphertext = service.Encrypt("sensitive-api-key", "tenant-a");

        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(ciphertext, "tenant-b"));
    }
}
