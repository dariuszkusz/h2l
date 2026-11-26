namespace H2l.Application.Groups.Commands;

public sealed record RemoveGroupMemberCommand(Guid GroupId, Guid UserId);
