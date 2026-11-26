using H2l.Domain.Identity;

namespace H2l.Application.Users.DTOs;

public sealed record CreateUserResponse(
    Guid Id,
    string Email,
    string? ExternalId,
    SystemRole SystemRole,
    bool IsActive,
    string DeliveryMethod,
    string? ActivationLink,
    DateTimeOffset CreatedAt);
