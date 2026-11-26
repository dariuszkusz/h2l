using H2l.Domain.Tasks;

namespace H2l.Application.Tasks.Queries;

public sealed record ListTasksQuery(
    HumanTaskStatus? Status,
    TaskListScope Scope,
    int Page,
    int PageSize);
