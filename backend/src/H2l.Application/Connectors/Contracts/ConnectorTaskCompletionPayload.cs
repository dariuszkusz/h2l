using System.Text.Json;
using H2l.Domain.Tasks;

namespace H2l.Application.Connectors.Contracts;

public sealed record ConnectorTaskCompletionPayload(
    Guid SubmissionId,
    IReadOnlyDictionary<string, JsonElement> PublicValues,
    IReadOnlyDictionary<string, JsonElement> SensitiveValues,
    IReadOnlyList<string> SensitiveKeys,
    SensitiveDataMode SensitiveDataMode);
