using System.ComponentModel.DataAnnotations;

namespace H2l.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = "h2l";

    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = "h2l-clients";

    [Required(ErrorMessage = "JWT SigningKey is required.")]
    [MinLength(32, ErrorMessage = "JWT SigningKey must be at least 32 characters long.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
