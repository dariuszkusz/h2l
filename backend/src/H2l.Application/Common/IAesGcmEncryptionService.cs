namespace H2l.Application.Common;

public interface IAesGcmEncryptionService
{
    string Encrypt(string plaintext, string? associatedData = null);

    string Decrypt(string ciphertext, string? associatedData = null);

    byte[] EncryptBytes(ReadOnlySpan<byte> plaintext, string? associatedData = null);

    byte[] DecryptBytes(ReadOnlySpan<byte> ciphertext, string? associatedData = null);
}
