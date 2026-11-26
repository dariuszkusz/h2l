namespace H2l.Application.Tasks.DTOs;

public sealed record TaskListResponse(
    IReadOnlyList<TaskSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
