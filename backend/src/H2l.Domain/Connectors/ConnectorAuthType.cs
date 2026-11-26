namespace H2l.Domain.Connectors;

public enum ConnectorAuthType
{
    None = 0,
    Basic = 1,
    Bearer = 2,
    ApiKey = 3,
    HmacSha256 = 4,
    OidcClientCredentials = 5,
    OAuth2ClientCredentials = 6
}
