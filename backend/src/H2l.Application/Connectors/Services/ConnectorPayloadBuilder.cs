using System.Text.Json;
using H2l.Application.Connectors.Contracts;
using H2l.Domain.Tasks;

namespace H2l.Application.Connectors.Services;

public sealed class ConnectorPayloadBuilder : IConnectorPayloadBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string BuildWebhookCompletionBody(
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload)
    {
        var data = BuildDataMap(payload);
        var metadata = new Dictionary<string, object?>
        {
            ["taskId"] = taskContext.TaskId,
            ["submissionId"] = payload.SubmissionId,
            ["formVersion"] = taskContext.FormVersionNumber,
            ["sensitiveKeys"] = payload.SensitiveKeys
        };

        var envelope = new Dictionary<string, object?>
        {
            ["status"] = "completed",
            ["data"] = data,
            ["_h2l"] = metadata
        };

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public string BuildWebhookCancellationBody(
        ConnectorTaskContext taskContext,
        string status,
        string reason)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["status"] = status.ToLowerInvariant(),
            ["data"] = new Dictionary<string, object?>(),
            ["_h2l"] = new Dictionary<string, object?>
            {
                ["taskId"] = taskContext.TaskId,
                ["reason"] = reason
            }
        };

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public Dictionary<string, object?> BuildCamundaCompletionVariables(
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload)
    {
        var variables = BuildDataMap(payload);

        if (payload.SensitiveDataMode == SensitiveDataMode.ClaimCheck)
        {
            variables["_h2l"] = new Dictionary<string, object?>
            {
                ["taskId"] = taskContext.TaskId,
                ["submissionId"] = payload.SubmissionId,
                ["formVersion"] = taskContext.FormVersionNumber,
                ["sensitiveKeys"] = payload.SensitiveKeys
            };
        }

        return variables;
    }

    private static Dictionary<string, object?> BuildDataMap(ConnectorTaskCompletionPayload payload)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in payload.PublicValues)
        {
            data[key] = value;
        }

        if (payload.SensitiveDataMode == SensitiveDataMode.PassThrough)
        {
            foreach (var (key, value) in payload.SensitiveValues)
            {
                data[key] = value;
            }
        }

        return data;
    }
}
