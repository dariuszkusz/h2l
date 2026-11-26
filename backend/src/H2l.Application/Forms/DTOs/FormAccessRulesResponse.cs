namespace H2l.Application.Forms.DTOs;

public sealed record FormAccessRulesResponse(
    Guid FormId,
    IReadOnlyList<FormAccessRuleItem> Rules);
