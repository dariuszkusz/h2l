using H2l.Domain.Identity;

namespace H2l.Application.Users.Commands;

public sealed record CreateUserRequest(
    string Email,
    string? ExternalId,
    SystemRole SystemRole,
    IReadOnlyList<Guid>? GroupIds);
