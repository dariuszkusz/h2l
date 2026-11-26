using System.Text.Json;

namespace H2l.Application.Forms.Schema;

public sealed record ValidatedSubmissionPayload(
    IReadOnlyDictionary<string, JsonElement> PublicValues,
    IReadOnlyDictionary<string, JsonElement> SensitiveValues);
