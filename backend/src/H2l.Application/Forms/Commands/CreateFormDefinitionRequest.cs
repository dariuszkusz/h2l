using System.Text.Json;

namespace H2l.Application.Forms.Commands;

public sealed record CreateFormDefinitionRequest(
    string Name,
    string StepKey,
    JsonElement? DraftSchemaJsonb);
