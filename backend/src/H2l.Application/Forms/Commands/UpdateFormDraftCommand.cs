using System.Text.Json;

namespace H2l.Application.Forms.Commands;

public sealed record UpdateFormDraftCommand(
    Guid FormId,
    string? Name,
    JsonElement DraftSchema);
