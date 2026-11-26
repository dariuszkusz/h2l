using H2l.Application.Outbox.Commands;
using H2l.Application.Outbox.DTOs;
using H2l.Application.Outbox.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/outbox")]
[Tags("OutboxAndDeadLetter")]
[Authorize(Roles = "Admin")]
public sealed class OutboxController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public OutboxController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("dead-letters")]
    [ProducesResponseType(typeof(DeadLetterListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> ListDeadLetters(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<DeadLetterListResponse>>(
            new ListDeadLettersQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("dead-letters/{outboxId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RetryDeadLetter(
        [FromRoute] Guid outboxId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result>(
            new RetryDeadLetterCommand(outboxId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblemResult(result.Error);
        }

        return Accepted();
    }
}
