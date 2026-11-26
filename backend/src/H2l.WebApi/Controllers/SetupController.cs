using H2l.Application.Setup.Commands;
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

    [HttpPost("initialize")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InitializeSetupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Initialize(
        [FromBody] InitializeSetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<InitializeSetupResponse>>(
            new InitializeSetupCommand(
                request.AdminEmail,
                request.AdminPassword,
                request.DefaultLanguage,
                request.SelectedPalette,
                request.Smtp),
            cancellationToken);

        return ToCreatedResult(result, "/api/v1/setup/status");
    }
}
