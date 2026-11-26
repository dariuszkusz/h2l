namespace H2l.Infrastructure.Connectors.Security;

public interface IConnectorHttpClientFactory
{
    HttpClient CreateClient(string? customCaCertificatePem, TimeSpan? timeout = null);
}
