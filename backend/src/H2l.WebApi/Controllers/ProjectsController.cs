using H2l.Application.Projects.Commands;
using H2l.Application.Projects.DTOs;
using H2l.Application.Projects.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/projects")]
[Tags("ProjectsAndProcesses")]
[Authorize]
public sealed class ProjectsController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public ProjectsController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> ListProjects(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<ProjectResponse>>>(
            new ListProjectsQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<ProjectResponse>>(
            new CreateProjectCommand(request.Name, request.Description),
            cancellationToken);

        return ToCreatedResult(result, result.IsSuccess ? $"/api/v1/projects/{result.Value.Id}" : null);
    }

    [HttpGet("{projectId:guid}/processes")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowProcessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ListProjectProcesses(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<WorkflowProcessResponse>>>(
            new ListProjectProcessesQuery(projectId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{projectId:guid}/processes")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(WorkflowProcessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateWorkflowProcess(
        [FromRoute] Guid projectId,
        [FromBody] CreateWorkflowProcessRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<WorkflowProcessResponse>>(
            new CreateWorkflowProcessCommand(
                projectId,
                request.Name,
                request.ExternalProcessKey,
                request.ConnectorId,
                request.Status),
            cancellationToken);

        return ToCreatedResult(
            result,
            result.IsSuccess ? $"/api/v1/projects/{projectId}/processes/{result.Value.Id}" : null);
    }
}
