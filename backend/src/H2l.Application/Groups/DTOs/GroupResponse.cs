namespace H2l.Application.Groups.DTOs;

public sealed record GroupResponse(
    Guid Id,
    string Name,
    string Code,
    int MemberCount,
    DateTimeOffset CreatedAt);
