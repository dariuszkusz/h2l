using System.ComponentModel.DataAnnotations;

namespace H2l.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(ErrorMessage = "Database connection string is required.")]
    public string ConnectionString { get; set; } = string.Empty;

    public bool AutoMigrate { get; set; } = true;
}
