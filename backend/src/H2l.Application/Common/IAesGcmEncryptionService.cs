namespace H2l.Application.Common;

public interface IAesGcmEncryptionService
{
    string Encrypt(string plaintext, string? associatedData = null);

    string Decrypt(string ciphertext, string? associatedData = null);
}
