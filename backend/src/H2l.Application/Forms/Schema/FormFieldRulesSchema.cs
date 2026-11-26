namespace H2l.Application.Forms.Schema;

public sealed record FormFieldRulesSchema(
    string? VisibleIf = null,
    string? RequiredIf = null,
    string? ReadOnlyIf = null);
