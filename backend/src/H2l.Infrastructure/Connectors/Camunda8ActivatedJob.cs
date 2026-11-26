using System.Text.Json;

namespace H2l.Infrastructure.Connectors;

public sealed record Camunda8ActivatedJob(
    string JobKey,
    IReadOnlyDictionary<string, string> CustomHeaders,
    IReadOnlyDictionary<string, JsonElement> Variables);
