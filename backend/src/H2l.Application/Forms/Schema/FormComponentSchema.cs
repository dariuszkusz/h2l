using System.Text.Json;

namespace H2l.Application.Forms.Schema;

public sealed record FormComponentSchema(
    string Id,
    string Type,
    string? VariableKey = null,
    LocalizedTextSchema? Label = null,
    LocalizedTextSchema? Description = null,
    LocalizedTextSchema? Placeholder = null,
    JsonElement? DefaultValue = null,
    bool? PreFilled = null,
    bool? ReadOnly = null,
    bool? IsSensitive = null,
    int? Columns = null,
    IReadOnlyList<FormFieldOptionSchema>? Options = null,
    FormFieldRulesSchema? Rules = null,
    IReadOnlyList<FormFieldValidatorSchema>? Validators = null,
    FormFileConfigSchema? FileConfig = null,
    IReadOnlyList<FormComponentSchema>? Components = null);
