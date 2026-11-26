using System.Text.Json;

namespace H2l.Application.Forms.DTOs;

public sealed record FormVersionResponse(
    Guid Id,
    Guid FormDefinitionId,
    int VersionNumber,
    DateTimeOffset PublishedAt,
    Guid PublishedByUserId,
    JsonElement Schema);
