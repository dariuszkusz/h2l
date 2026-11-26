using System.Text.Json;

namespace H2l.Application.Tasks.Commands;

public sealed record SubmitTaskCommand(
    Guid TaskId,
    string IdempotencyKey,
    IReadOnlyDictionary<string, JsonElement> Values,
    IReadOnlyList<Guid>? AttachmentIds = null);
