using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using H2l.Application.Licensing;
using H2l.Domain.Common;
using H2l.Domain.Licensing;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace H2l.Infrastructure.Licensing;

public sealed class Ed25519LicenseVerifier : ILicenseVerifier
{
    private const string LicenseFormatPrefix = "H2L1";
    private const int Ed25519SignatureLengthBytes = 64;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ILicensePublicKeyProvider _publicKeyProvider;

    public Ed25519LicenseVerifier(ILicensePublicKeyProvider publicKeyProvider)
    {
        _publicKeyProvider = publicKeyProvider;
    }

    public Result<VerifiedLicense> Verify(
        string licenseKey,
        Guid expectedInstanceId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_KEY_REQUIRED", "License key is required."));
        }

        if (expectedInstanceId == Guid.Empty)
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("INSTANCE_ID_REQUIRED", "Instance identifier is required."));
        }

        var publicKeyBytes = _publicKeyProvider.GetPublicKeyBytes();
        if (publicKeyBytes is null)
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_PUBLIC_KEY_NOT_CONFIGURED", "License verification public key is not configured."));
        }

        var parts = licenseKey.Trim().Split('.');
        if (parts.Length != 3 || !string.Equals(parts[0], LicenseFormatPrefix, StringComparison.Ordinal))
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_FORMAT_INVALID", "License key format is invalid."));
        }

        if (!TryDecodeBase64Url(parts[1], out var payloadBytes) ||
            !TryDecodeBase64Url(parts[2], out var signatureBytes) ||
            signatureBytes.Length != Ed25519SignatureLengthBytes)
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_FORMAT_INVALID", "License key encoding is invalid."));
        }

        var signedContent = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        var signer = new Ed25519Signer();
        signer.Init(false, new Ed25519PublicKeyParameters(publicKeyBytes, 0));
        signer.BlockUpdate(signedContent, 0, signedContent.Length);

        if (!signer.VerifySignature(signatureBytes))
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_SIGNATURE_INVALID", "License key signature verification failed."));
        }

        LicensePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes, SerializerOptions);
        }
        catch (JsonException)
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_PAYLOAD_INVALID", "License payload could not be parsed."));
        }

        if (payload is null || payload.LicenseId == Guid.Empty || payload.InstanceId == Guid.Empty)
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation("LICENSE_PAYLOAD_INVALID", "License payload is missing required fields."));
        }

        if (payload.InstanceId != expectedInstanceId)
        {
            return Result.Failure<VerifiedLicense>(
                Error.Validation(
                    "LICENSE_INSTANCE_MISMATCH",
                    "License key is bound to a different InstanceId and cannot be activated on this instance."));
        }

        var graceDays = payload.GracePeriodDays < 0 ? 0 : payload.GracePeriodDays;
        DateTimeOffset? graceEndsAt = null;
        LicenseStatus status;
        bool inGracePeriod;

        if (payload.ExpiresAt is null || now <= payload.ExpiresAt.Value)
        {
            status = LicenseStatus.Active;
            inGracePeriod = false;
        }
        else
        {
            graceEndsAt = payload.ExpiresAt.Value.AddDays(graceDays);
            if (now <= graceEndsAt.Value)
            {
                status = LicenseStatus.GracePeriod;
                inGracePeriod = true;
            }
            else
            {
                status = LicenseStatus.Expired;
                inGracePeriod = false;
            }
        }

        return Result.Success(new VerifiedLicense(payload, status, graceEndsAt, inGracePeriod));
    }

    private static bool TryDecodeBase64Url(string input, out byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            bytes = [];
            return false;
        }

        var padded = input.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder == 2)
        {
            padded += "==";
        }
        else if (remainder == 3)
        {
            padded += "=";
        }
        else if (remainder == 1)
        {
            bytes = [];
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
