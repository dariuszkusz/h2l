using System.Text.Json;

namespace H2l.Application.Connectors.DTOs;

public sealed record N8nNodesTemplateResponse(
    string Variant,
    JsonElement WorkflowJson);
