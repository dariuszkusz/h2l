using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using H2l.Application.Connectors.Contracts;
using H2l.Domain.Common;

namespace H2l.Infrastructure.Connectors.Security;

public sealed class HmacWebhookSigner : IHmacWebhookSigner
{
    public const long MaxReplayWindowSeconds = 300;
    public const string SignaturePrefix = "sha256=";

    public string ComputeSignature(string secret, long timestampUtcSeconds, string rawBody)
    {
        if (string.IsNullOrEmpty(secret))
        {
            throw new ArgumentException("HMAC secret must not be empty.", nameof(secret));
        }

        var payload = $"{timestampUtcSeconds.ToString(CultureInfo.InvariantCulture)}.{rawBody}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);

        return SignaturePrefix + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public Result VerifySignature(
        string secret,
        string? timestampHeader,
        string? signatureHeader,
        string rawBody,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return Result.Failure(Error.Unauthorized("WEBHOOK_SECRET_NOT_CONFIGURED", "Connector HMAC secret is not configured."));
        }

        if (string.IsNullOrWhiteSpace(timestampHeader) ||
            !long.TryParse(timestampHeader.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return Result.Failure(Error.Unauthorized("WEBHOOK_TIMESTAMP_INVALID", "X-H2L-Timestamp header is missing or invalid."));
        }

        var nowSeconds = utcNow.ToUnixTimeSeconds();
        if (Math.Abs(nowSeconds - timestampSeconds) > MaxReplayWindowSeconds)
        {
            return Result.Failure(Error.Unauthorized(
                "WEBHOOK_TIMESTAMP_EXPIRED",
                "Webhook timestamp is outside the allowed ±300 seconds replay window."));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader) ||
            !signatureHeader.Trim().StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Unauthorized("WEBHOOK_SIGNATURE_INVALID", "X-H2L-Signature header is missing or malformed."));
        }

        var expected = ComputeSignature(secret, timestampSeconds, rawBody);
        var providedNormalized = signatureHeader.Trim().ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(providedNormalized);

        if (expectedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            return Result.Failure(Error.Unauthorized("WEBHOOK_SIGNATURE_MISMATCH", "Webhook HMAC signature verification failed."));
        }

        return Result.Success();
    }
}
