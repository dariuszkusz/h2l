using H2l.Application.Licensing;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace H2l.Infrastructure.Licensing;

public sealed class ConfiguredLicensePublicKeyProvider : ILicensePublicKeyProvider
{
    private const int Ed25519PublicKeyLengthBytes = 32;

    // Production self-hosted release builds can embed the official Ed25519 public key here
    // (or override via LICENSE_PUBLIC_KEY_BASE64 / Licensing:PublicKeyBase64).
    // Private Ed25519 signing keys NEVER exist in this repository.
    private const string EmbeddedProductionPublicKeyBase64 = "";

    private readonly LicensingOptions _options;

    public ConfiguredLicensePublicKeyProvider(IOptions<LicensingOptions> options)
    {
        _options = options.Value;
    }

    public byte[]? GetPublicKeyBytes()
    {
        var candidate = !string.IsNullOrWhiteSpace(_options.PublicKeyBase64)
            ? _options.PublicKeyBase64.Trim()
            : EmbeddedProductionPublicKeyBase64;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(candidate);
            return bytes.Length == Ed25519PublicKeyLengthBytes ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
