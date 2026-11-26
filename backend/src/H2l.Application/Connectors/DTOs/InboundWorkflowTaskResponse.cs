using H2l.Domain.Tasks;

namespace H2l.Application.Connectors.DTOs;

public sealed record InboundWorkflowTaskResponse(
    Guid TaskId,
    HumanTaskStatus Status,
    bool IsDuplicate);
