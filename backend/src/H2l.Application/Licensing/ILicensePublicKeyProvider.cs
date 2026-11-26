namespace H2l.Application.Licensing;

public interface ILicensePublicKeyProvider
{
    byte[]? GetPublicKeyBytes();
}
