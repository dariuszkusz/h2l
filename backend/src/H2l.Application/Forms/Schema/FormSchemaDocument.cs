namespace H2l.Application.Forms.Schema;

public sealed record FormSchemaDocument(
    string SchemaVersion,
    string DefaultLanguage,
    string LayoutMode,
    IReadOnlyList<FormStepSchema> Steps);
