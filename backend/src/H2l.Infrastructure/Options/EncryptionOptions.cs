using System.ComponentModel.DataAnnotations;

namespace H2l.Infrastructure.Options;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    [Required(ErrorMessage = "Encryption KeyId is required.")]
    public string KeyId { get; set; } = "v1";

    [Required(ErrorMessage = "Encryption AesGcmKey is required.")]
    public string AesGcmKey { get; set; } = string.Empty;
}
