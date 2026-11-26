namespace H2l.Application.Forms.Commands;

public sealed record UpdateFormAccessRulesCommand(
    Guid FormId,
    IReadOnlyList<UpdateFormAccessRuleItemRequest> Rules);
