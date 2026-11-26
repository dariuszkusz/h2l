using System.Security.Cryptography;
using System.Text;
using H2l.Application.Common;

namespace H2l.Infrastructure.Security;

public sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string ComputeSha256Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var bytes = Encoding.UTF8.GetBytes(rawToken.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
