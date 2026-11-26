using System.Text.Json;

namespace H2l.Application.Forms.Commands;

public sealed record UpdateFormDraftRequest(
    string? Name,
    JsonElement DraftSchema);
