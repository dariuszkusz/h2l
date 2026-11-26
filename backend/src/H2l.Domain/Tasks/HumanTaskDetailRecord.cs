using H2l.Domain.Workflows;

namespace H2l.Domain.Tasks;

public sealed record HumanTaskDetailRecord(
    HumanTask Task,
    FormVersion FormVersion,
    Guid FormDefinitionId);
