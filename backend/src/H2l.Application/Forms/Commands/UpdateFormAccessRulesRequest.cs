namespace H2l.Application.Forms.Commands;

public sealed record UpdateFormAccessRulesRequest(
    IReadOnlyList<UpdateFormAccessRuleItemRequest> Rules);
