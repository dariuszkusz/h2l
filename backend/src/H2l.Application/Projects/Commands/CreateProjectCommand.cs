namespace H2l.Application.Projects.Commands;

public sealed record CreateProjectCommand(
    string Name,
    string? Description);
