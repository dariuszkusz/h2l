using System.Text.Json;

namespace H2l.Application.Tasks.Commands;

public sealed record SubmitTaskRequest(
    Dictionary<string, JsonElement> Values,
    IReadOnlyList<Guid>? AttachmentIds = null);
