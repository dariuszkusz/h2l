namespace H2l.Application.Forms.Queries;

public sealed record GetCamundaElementTemplateQuery(
    Guid FormId,
    bool Generic = false);
