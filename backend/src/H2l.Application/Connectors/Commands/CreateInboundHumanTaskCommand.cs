using System.Text.Json;
using H2l.Domain.Tasks;

namespace H2l.Application.Connectors.Commands;

public sealed record CreateInboundHumanTaskCommand(
    Guid ConnectorId,
    string ExternalTaskId,
    Guid FormId,
    int? FormVersion,
    string? Assignee,
    IReadOnlyList<string>? CandidateGroups,
    IReadOnlyDictionary<string, JsonElement>? PrefilledData,
    string? ResumeUrl,
    string? TimeoutIso8601,
    SensitiveDataMode? SensitiveDataMode,
    bool ThrowOnMissingAssignee = false);
