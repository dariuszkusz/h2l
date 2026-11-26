using Microsoft.Extensions.Options;

namespace H2l.Infrastructure.Options;

public sealed class EncryptionOptionsValidator : IValidateOptions<EncryptionOptions>
{
    private const int RequiredKeyLengthBytes = 32;

    public ValidateOptionsResult Validate(string? name, EncryptionOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.KeyId))
        {
            failures.Add("Encryption KeyId is required.");
        }
        else if (options.KeyId.Contains(':', StringComparison.Ordinal))
        {
            failures.Add("Encryption KeyId cannot contain ':'.");
        }

        if (string.IsNullOrWhiteSpace(options.AesGcmKey))
        {
            failures.Add("Encryption AesGcmKey is required.");
        }
        else if (options.AesGcmKey.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Encryption AesGcmKey must not use a placeholder value.");
        }
        else
        {
            try
            {
                var keyBytes = Convert.FromBase64String(options.AesGcmKey);
                if (keyBytes.Length != RequiredKeyLengthBytes)
                {
                    failures.Add($"Encryption AesGcmKey must decode to exactly {RequiredKeyLengthBytes} bytes (256-bit AES-GCM).");
                }
            }
            catch (FormatException)
            {
                failures.Add("Encryption AesGcmKey must be a valid Base64 string.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
