namespace H2l.Application.Forms.Schema;

public sealed record FormFileConfigSchema(
    IReadOnlyList<string>? AllowedMimeTypes = null,
    long? MaxSizeBytes = null,
    bool? Multiple = null);
