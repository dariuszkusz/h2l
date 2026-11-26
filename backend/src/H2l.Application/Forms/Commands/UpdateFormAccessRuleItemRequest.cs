namespace H2l.Application.Forms.Commands;

public sealed record UpdateFormAccessRuleItemRequest(
    Guid? GroupId,
    Guid? UserId);
