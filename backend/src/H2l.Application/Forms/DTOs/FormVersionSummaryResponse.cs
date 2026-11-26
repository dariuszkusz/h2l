using System.Text.Json;

namespace H2l.Application.Forms.DTOs;

public sealed record FormVersionSummaryResponse(
    Guid Id,
    int VersionNumber,
    DateTimeOffset PublishedAt,
    Guid PublishedByUserId,
    JsonElement Schema);
