using H2l.Domain.Workflows;

namespace H2l.Application.Projects.Commands;

public sealed record CreateWorkflowProcessCommand(
    Guid ProjectId,
    string Name,
    string ExternalProcessKey,
    Guid? ConnectorId,
    WorkflowProcessStatus? Status);
