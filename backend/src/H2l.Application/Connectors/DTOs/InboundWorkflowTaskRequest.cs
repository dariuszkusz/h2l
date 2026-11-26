using System.Text.Json;
using H2l.Domain.Tasks;

namespace H2l.Application.Connectors.DTOs;

public sealed record InboundWorkflowTaskRequest(
    string ExternalTaskId,
    Guid FormId,
    int? FormVersion = null,
    string? Assignee = null,
    IReadOnlyList<string>? CandidateGroups = null,
    IReadOnlyDictionary<string, JsonElement>? PrefilledData = null,
    string? ResumeUrl = null,
    string? TimeoutIso8601 = null,
    SensitiveDataMode? SensitiveDataMode = null);
