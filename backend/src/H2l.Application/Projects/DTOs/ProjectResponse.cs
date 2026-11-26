namespace H2l.Application.Projects.DTOs;

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt);
