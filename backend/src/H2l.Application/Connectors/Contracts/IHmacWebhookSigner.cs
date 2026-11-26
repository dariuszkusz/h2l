using H2l.Domain.Common;

namespace H2l.Application.Connectors.Contracts;

public interface IHmacWebhookSigner
{
    string ComputeSignature(string secret, long timestampUtcSeconds, string rawBody);

    Result VerifySignature(
        string secret,
        string? timestampHeader,
        string? signatureHeader,
        string rawBody,
        DateTimeOffset utcNow);
}
