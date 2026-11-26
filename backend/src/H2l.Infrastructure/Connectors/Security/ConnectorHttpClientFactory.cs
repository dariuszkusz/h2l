namespace H2l.Infrastructure.Connectors.Security;

public sealed class ConnectorHttpClientFactory : IConnectorHttpClientFactory
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public HttpClient CreateClient(string? customCaCertificatePem, TimeSpan? timeout = null)
    {
        var handler = CustomCaTlsHelper.CreateHandler(customCaCertificatePem);
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? DefaultTimeout
        };
    }
}
