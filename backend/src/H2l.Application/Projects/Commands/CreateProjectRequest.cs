namespace H2l.Application.Projects.Commands;

public sealed record CreateProjectRequest(
    string Name,
    string? Description);
