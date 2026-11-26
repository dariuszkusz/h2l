using H2l.Domain.Workflows;

namespace H2l.Application.Forms.DTOs;

public sealed record FormAccessRuleItem(
    Guid Id,
    Guid? GroupId,
    Guid? UserId,
    FormAccessScope AccessScope);
