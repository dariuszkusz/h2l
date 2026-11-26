namespace H2l.Domain.Tasks;

public sealed record PagedHumanTasksResult(
    IReadOnlyList<HumanTaskListItem> Items,
    int TotalCount);
