using System.Security.Cryptography;
using System.Text;
using H2l.Application.Common;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace H2l.Infrastructure.Security;

public sealed class AesGcmEncryptionService : IAesGcmEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly string _keyId;
    private readonly byte[] _keyBytes;

    public AesGcmEncryptionService(IOptions<EncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value;

        if (string.IsNullOrWhiteSpace(value.KeyId) || value.KeyId.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Encryption KeyId is invalid.");
        }

        _keyId = value.KeyId.Trim();
        _keyBytes = Convert.FromBase64String(value.AesGcmKey);
        if (_keyBytes.Length != 32)
        {
            throw new InvalidOperationException("Encryption AesGcmKey must be 32 bytes (256-bit).");
        }
    }

    public string Encrypt(string plaintext, string? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];
        var aadBytes = string.IsNullOrEmpty(associatedData)
            ? []
            : Encoding.UTF8.GetBytes(associatedData);

        using var aesGcm = new AesGcm(_keyBytes, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag, aadBytes);

        return $"{_keyId}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(ciphertext)}";
    }

    public string Decrypt(string ciphertext, string? associatedData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ciphertext);

        var parts = ciphertext.Split(':');
        if (parts.Length != 4)
        {
            throw new CryptographicException("Encrypted payload format is invalid.");
        }

        if (!string.Equals(parts[0], _keyId, StringComparison.Ordinal))
        {
            throw new CryptographicException("Encrypted payload key identifier does not match active key.");
        }

        byte[] nonce;
        byte[] tag;
        byte[] encryptedBytes;
        try
        {
            nonce = Convert.FromBase64String(parts[1]);
            tag = Convert.FromBase64String(parts[2]);
            encryptedBytes = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Encrypted payload contains invalid Base64 data.", ex);
        }

        if (nonce.Length != NonceSizeBytes || tag.Length != TagSizeBytes)
        {
            throw new CryptographicException("Encrypted payload nonce or tag length is invalid.");
        }

        var plaintextBytes = new byte[encryptedBytes.Length];
        var aadBytes = string.IsNullOrEmpty(associatedData)
            ? []
            : Encoding.UTF8.GetBytes(associatedData);

        using var aesGcm = new AesGcm(_keyBytes, TagSizeBytes);
        aesGcm.Decrypt(nonce, encryptedBytes, tag, plaintextBytes, aadBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public byte[] EncryptBytes(ReadOnlySpan<byte> plaintext, string? associatedData = null)
    {
        var keyIdBytes = Encoding.UTF8.GetBytes(_keyId);
        if (keyIdBytes.Length > byte.MaxValue)
        {
            throw new InvalidOperationException("Encryption KeyId exceeds 255 bytes.");
        }

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[plaintext.Length];
        var aadBytes = string.IsNullOrEmpty(associatedData)
            ? []
            : Encoding.UTF8.GetBytes(associatedData);

        using var aesGcm = new AesGcm(_keyBytes, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, aadBytes);

        var output = new byte[1 + keyIdBytes.Length + NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        var offset = 0;
        output[offset++] = (byte)keyIdBytes.Length;
        Buffer.BlockCopy(keyIdBytes, 0, output, offset, keyIdBytes.Length);
        offset += keyIdBytes.Length;
        Buffer.BlockCopy(nonce, 0, output, offset, NonceSizeBytes);
        offset += NonceSizeBytes;
        Buffer.BlockCopy(tag, 0, output, offset, TagSizeBytes);
        offset += TagSizeBytes;
        Buffer.BlockCopy(ciphertext, 0, output, offset, ciphertext.Length);

        return output;
    }

    public byte[] DecryptBytes(ReadOnlySpan<byte> ciphertext, string? associatedData = null)
    {
        if (ciphertext.Length < 1 + NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Encrypted binary payload is truncated.");
        }

        var keyIdLen = ciphertext[0];
        var headerLen = 1 + keyIdLen + NonceSizeBytes + TagSizeBytes;
        if (keyIdLen == 0 || ciphertext.Length < headerLen)
        {
            throw new CryptographicException("Encrypted binary payload header is invalid.");
        }

        var keyId = Encoding.UTF8.GetString(ciphertext.Slice(1, keyIdLen));
        if (!string.Equals(keyId, _keyId, StringComparison.Ordinal))
        {
            throw new CryptographicException("Encrypted binary payload key identifier does not match active key.");
        }

        var nonceOffset = 1 + keyIdLen;
        var tagOffset = nonceOffset + NonceSizeBytes;
        var dataOffset = tagOffset + TagSizeBytes;

        var nonce = ciphertext.Slice(nonceOffset, NonceSizeBytes);
        var tag = ciphertext.Slice(tagOffset, TagSizeBytes);
        var encryptedData = ciphertext.Slice(dataOffset);
        var plaintext = new byte[encryptedData.Length];
        var aadBytes = string.IsNullOrEmpty(associatedData)
            ? []
            : Encoding.UTF8.GetBytes(associatedData);

        using var aesGcm = new AesGcm(_keyBytes, TagSizeBytes);
        aesGcm.Decrypt(nonce, encryptedData, tag, plaintext, aadBytes);

        return plaintext;
    }
}
