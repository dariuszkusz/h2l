using H2l.Domain.Tasks;

namespace H2l.Application.Tasks.DTOs;

public sealed record SubmitTaskResponse(
    Guid TaskId,
    Guid SubmissionId,
    HumanTaskStatus Status,
    DateTimeOffset SubmittedAt);
