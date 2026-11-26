using System.Text.Json;

namespace H2l.Application.Forms.Commands;

public sealed record CreateFormDefinitionCommand(
    Guid ProcessId,
    string Name,
    string StepKey,
    JsonElement? DraftSchemaJsonb);
