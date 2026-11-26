using System.Text.Json;
using H2l.Application.Tasks.DTOs;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Tasks;

public static class TaskDetailResponseMapper
{
    public static TaskDetailResponse ToDetailResponse(HumanTask task, FormVersion formVersion, Guid currentUserId)
    {
        using var schemaDoc = JsonDocument.Parse(formVersion.SchemaJsonb);
        JsonElement prefilled;
        if (string.IsNullOrWhiteSpace(task.PrefilledDataJsonb))
        {
            using var emptyDoc = JsonDocument.Parse("{}");
            prefilled = emptyDoc.RootElement.Clone();
        }
        else
        {
            using var prefilledDoc = JsonDocument.Parse(task.PrefilledDataJsonb);
            prefilled = prefilledDoc.RootElement.Clone();
        }

        return new TaskDetailResponse(
            task.Id,
            task.Status,
            formVersion.Id,
            formVersion.VersionNumber,
            schemaDoc.RootElement.Clone(),
            prefilled,
            task.AssignedUserId,
            task.ClaimedByUserId,
            task.CanClaim(currentUserId),
            task.CanUnclaim(currentUserId),
            task.SensitiveDataMode,
            task.ExpiresAt,
            task.CreatedAt,
            task.CompletedAt);
    }
}
