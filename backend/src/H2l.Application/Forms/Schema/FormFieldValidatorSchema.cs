using System.Text.Json;

namespace H2l.Application.Forms.Schema;

public sealed record FormFieldValidatorSchema(
    string Type,
    JsonElement? Value = null,
    string? Pattern = null,
    string? TargetField = null,
    string? Operator = null,
    string? Expression = null,
    LocalizedTextSchema? Message = null);
