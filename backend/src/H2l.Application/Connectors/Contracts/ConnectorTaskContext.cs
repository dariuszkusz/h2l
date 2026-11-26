using H2l.Domain.Tasks;

namespace H2l.Application.Connectors.Contracts;

public sealed record ConnectorTaskContext(
    Guid TenantId,
    Guid ConnectorId,
    Guid TaskId,
    string ExternalTaskId,
    int FormVersionNumber,
    SensitiveDataMode SensitiveDataMode,
    string? ResumeUrl,
    string IdempotencyKey);
