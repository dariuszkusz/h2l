using System.Text.Json;

namespace H2l.Application.Submissions.DTOs;

public sealed record SensitiveDataResponse(
    Guid SubmissionId,
    JsonElement Values);
