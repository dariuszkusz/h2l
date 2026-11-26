using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using H2l.Application.Licensing;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace H2l.UnitTests.Licensing;

public sealed class TestEd25519LicenseSigner : ILicensePublicKeyProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Ed25519PrivateKeyParameters _privateKey;
    private readonly Ed25519PublicKeyParameters _publicKey;

    public TestEd25519LicenseSigner()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = generator.GenerateKeyPair();
        _privateKey = (Ed25519PrivateKeyParameters)keyPair.Private;
        _publicKey = (Ed25519PublicKeyParameters)keyPair.Public;
    }

    public byte[] GetPublicKeyBytes() => _publicKey.GetEncoded();

    public string Sign(LicensePayload payload)
    {
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
        var encodedPayload = ToBase64Url(payloadJson);
        var signingInput = $"H2L1.{encodedPayload}";
        var signingBytes = Encoding.UTF8.GetBytes(signingInput);

        var signer = new Ed25519Signer();
        signer.Init(true, _privateKey);
        signer.BlockUpdate(signingBytes, 0, signingBytes.Length);
        var signature = signer.GenerateSignature();

        return $"{signingInput}.{ToBase64Url(signature)}";
    }

    private static string ToBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
