using H2l.Application.Submissions.DTOs;
using H2l.Application.Submissions.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/submissions")]
[Tags("SensitiveData")]
[Authorize(Roles = "Admin,ProcessDesigner")]
public sealed class SubmissionsController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public SubmissionsController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("{submissionId:guid}/sensitive-data")]
    [ProducesResponseType(typeof(SensitiveDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetSubmissionSensitiveData(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<SensitiveDataResponse>>(
            new GetSubmissionSensitiveDataQuery(submissionId),
            cancellationToken);

        return ToActionResult(result);
    }
}
