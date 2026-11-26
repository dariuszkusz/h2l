namespace H2l.Infrastructure.Options;

public sealed class LicensingOptions
{
    public const string SectionName = "Licensing";

    public string? PublicKeyBase64 { get; set; }
}
