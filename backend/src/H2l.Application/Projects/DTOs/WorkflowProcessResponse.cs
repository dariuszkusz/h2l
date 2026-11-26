using H2l.Domain.Workflows;

namespace H2l.Application.Projects.DTOs;

public sealed record WorkflowProcessResponse(
    Guid Id,
    Guid ProjectId,
    Guid? ConnectorId,
    string Name,
    string ExternalProcessKey,
    WorkflowProcessStatus Status,
    DateTimeOffset CreatedAt);
