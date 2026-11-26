namespace H2l.Application.Forms.Queries;

public sealed record GetN8nNodesTemplateQuery(
    Guid FormId,
    string Variant = "community-node",
    Guid? ConnectorId = null);
