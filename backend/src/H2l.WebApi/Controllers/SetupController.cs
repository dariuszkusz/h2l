using H2l.Application.Setup.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/setup")]
[Tags("Setup")]
public sealed class SetupController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public SetupController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetupStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<SetupStatusResponse>>(
            new GetSetupStatusQuery(),
            cancellationToken);

        return ToActionResult(result);
    }
}
