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

    [HttpGet("forms/{formId:guid}")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(FormDefinitionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetFormDefinition(
        [FromRoute] Guid formId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<FormDefinitionDetailResponse>>(
            new GetFormDefinitionQuery(formId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("forms/{formId:guid}/draft")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(FormDefinitionDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateFormDraft(
        [FromRoute] Guid formId,
        [FromBody] UpdateFormDraftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<FormDefinitionDetailResponse>>(
            new UpdateFormDraftCommand(formId, request.Name, request.DraftSchema),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("forms/{formId:guid}/versions")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(IReadOnlyList<FormVersionSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ListFormVersions(
        [FromRoute] Guid formId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<FormVersionSummaryResponse>>>(
            new ListFormVersionsQuery(formId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("forms/{formId:guid}/publish")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(FormVersionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> PublishFormVersion(
        [FromRoute] Guid formId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<FormVersionResponse>>(
            new PublishFormVersionCommand(formId),
            cancellationToken);

        return ToCreatedResult(
            result,
            result.IsSuccess ? $"/api/v1/forms/{formId}/versions" : null);
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
