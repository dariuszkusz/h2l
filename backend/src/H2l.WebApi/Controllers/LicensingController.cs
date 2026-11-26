using H2l.Application.Licensing;
using H2l.Application.Licensing.Commands;
using H2l.Application.Licensing.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/licensing")]
[Tags("Licensing")]
[Authorize(Roles = "Admin")]
public sealed class LicensingController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public LicensingController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(LicensingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<LicensingStatusResponse>>(
            new GetLicensingStatusQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("activate")]
    [ProducesResponseType(typeof(LicensingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<LicensingStatusResponse>>(
            new ActivateLicenseCommand(request.LicenseKey),
            cancellationToken);

        return ToActionResult(result);
    }
}
