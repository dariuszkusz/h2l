using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace H2l.Infrastructure.Connectors.Security;

public static class CustomCaTlsHelper
{
    public static HttpClientHandler CreateHandler(string? customCaCertificatePem)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        if (string.IsNullOrWhiteSpace(customCaCertificatePem))
        {
            return handler;
        }

        var customRoots = new X509Certificate2Collection();
        customRoots.ImportFromPem(customCaCertificatePem);
        if (customRoots.Count == 0)
        {
            throw new InvalidOperationException("Custom CA PEM did not contain any valid X.509 certificates.");
        }

        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, sslPolicyErrors) =>
        {
            if (certificate is null)
            {
                return false;
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Never allow hostname mismatches or missing certificates — only custom root trust.
            if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None)
            {
                return false;
            }

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            foreach (var root in customRoots)
            {
                chain.ChainPolicy.CustomTrustStore.Add(root);
            }

            return chain.Build(certificate);
        };

        return handler;
    }
}
