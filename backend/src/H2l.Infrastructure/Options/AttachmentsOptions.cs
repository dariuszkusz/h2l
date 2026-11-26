using System.ComponentModel.DataAnnotations;

namespace H2l.Infrastructure.Options;

public sealed class AttachmentsOptions
{
    public const string SectionName = "Attachments";

    [Range(1, 250)]
    public int MaxSizeMb { get; set; } = 10;

    [Required]
    public string StoragePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "attachments");

    public string[] AllowedMimeTypes { get; set; } =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/webp",
        "text/plain",
        "text/csv",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ];
}
