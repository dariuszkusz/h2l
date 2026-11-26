namespace H2l.Application.Forms.Schema;

public sealed record FormStepSchema(
    string Id,
    LocalizedTextSchema? Title,
    LocalizedTextSchema? Description,
    IReadOnlyList<FormComponentSchema> Components);
