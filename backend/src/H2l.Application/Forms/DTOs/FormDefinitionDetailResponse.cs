using System.Text.Json;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.DTOs;

public sealed record FormDefinitionDetailResponse(
    Guid Id,
    Guid WorkflowProcessId,
    string Name,
    string StepKey,
    FormDefinitionStatus Status,
    JsonElement? DraftSchema,
    int? LatestVersionNumber,
    DateTimeOffset UpdatedAt);
