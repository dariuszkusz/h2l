using H2l.Application.Forms.Commands;
using H2l.Application.Forms.DTOs;
using H2l.Application.Forms.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1")]
[Tags("Forms")]
[Authorize]
public sealed class FormsController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public FormsController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("processes/{processId:guid}/forms")]
    [ProducesResponseType(typeof(IReadOnlyList<FormDefinitionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ListProcessForms(
        [FromRoute] Guid processId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<FormDefinitionResponse>>>(
            new ListProcessFormsQuery(processId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("processes/{processId:guid}/forms")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(FormDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateFormDefinition(
        [FromRoute] Guid processId,
        [FromBody] CreateFormDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<FormDefinitionResponse>>(
            new CreateFormDefinitionCommand(
                processId,
                request.Name,
                request.StepKey,
                request.DraftSchemaJsonb),
            cancellationToken);

        return ToCreatedResult(
            result,
            result.IsSuccess ? $"/api/v1/processes/{processId}/forms/{result.Value.Id}" : null);
    }

    [HttpGet("forms/{formId:guid}/access-rules")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(FormAccessRulesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetFormAccessRules(
        [FromRoute] Guid formId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<FormAccessRulesResponse>>(
            new GetFormAccessRulesQuery(formId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("forms/{formId:guid}/access-rules")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateFormAccessRules(
        [FromRoute] Guid formId,
        [FromBody] UpdateFormAccessRulesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result>(
            new UpdateFormAccessRulesCommand(formId, request.Rules),
            cancellationToken);

        return ToActionResult(result);
    }
}
