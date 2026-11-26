using H2l.Infrastructure.Connectors.Security;
using Xunit;

namespace H2l.UnitTests.Connectors;

public sealed class HmacWebhookSignerTests
{
    private readonly HmacWebhookSigner _sut = new();

    [Fact]
    public void VerifySignature_ValidWithinFiveMinutes_Succeeds()
    {
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddSeconds(-120).ToUnixTimeSeconds();
        const string secret = "super-secret-hmac-key";
        const string body = "{\"externalTaskId\":\"job-1\"}";

        var signature = _sut.ComputeSignature(secret, timestamp, body);
        var result = _sut.VerifySignature(secret, timestamp.ToString(), signature, body, now);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(-301)]
    [InlineData(301)]
    public void VerifySignature_OutsideFiveMinuteReplayWindow_Fails(int offsetSeconds)
    {
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.AddSeconds(offsetSeconds).ToUnixTimeSeconds();
        const string secret = "super-secret-hmac-key";
        const string body = "{\"externalTaskId\":\"job-1\"}";

        var signature = _sut.ComputeSignature(secret, timestamp, body);
        var result = _sut.VerifySignature(secret, timestamp.ToString(), signature, body, now);

        Assert.True(result.IsFailure);
        Assert.Equal("WEBHOOK_TIMESTAMP_EXPIRED", result.Error.Code);
    }

    [Fact]
    public void VerifySignature_TamperedBody_Fails()
    {
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var timestamp = now.ToUnixTimeSeconds();
        const string secret = "super-secret-hmac-key";

        var signature = _sut.ComputeSignature(secret, timestamp, "{\"externalTaskId\":\"job-1\"}");
        var result = _sut.VerifySignature(secret, timestamp.ToString(), signature, "{\"externalTaskId\":\"job-2\"}", now);

        Assert.True(result.IsFailure);
        Assert.Equal("WEBHOOK_SIGNATURE_MISMATCH", result.Error.Code);
    }
}
