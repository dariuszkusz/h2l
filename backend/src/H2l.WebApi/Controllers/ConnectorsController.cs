using H2l.Application.Connectors.Commands;
using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/connectors")]
[Tags("Connectors")]
[Authorize(Roles = "Admin")]
public sealed class ConnectorsController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public ConnectorsController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ConnectorListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> ListConnectors(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<ConnectorListResponse>>(
            new ListConnectorsQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ConnectorSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateConnector(
        [FromBody] CreateConnectorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<ConnectorSummaryResponse>>(
            new CreateConnectorCommand(
                request.Name,
                request.ConnectorType,
                request.BaseUrl,
                request.AuthType,
                request.Credentials,
                request.CustomCaCertificatePem,
                request.CallbackUrl,
                request.IsActive),
            cancellationToken);

        return ToCreatedResult(
            result,
            result.IsSuccess ? $"/api/v1/connectors/{result.Value.Id}" : null);
    }

    [HttpPut("{connectorId:guid}")]
    [ProducesResponseType(typeof(ConnectorSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateConnector(
        [FromRoute] Guid connectorId,
        [FromBody] UpdateConnectorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<ConnectorSummaryResponse>>(
            new UpdateConnectorCommand(
                connectorId,
                request.Name,
                request.ConnectorType,
                request.BaseUrl,
                request.AuthType,
                request.IsActive,
                request.Credentials,
                request.CustomCaCertificatePem,
                request.ClearCustomCaCertificate,
                request.CallbackUrl),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{connectorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteConnector(
        [FromRoute] Guid connectorId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result>(
            new DeleteConnectorCommand(connectorId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{connectorId:guid}/test")]
    [ProducesResponseType(typeof(TestConnectorConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> TestConnectorConnection(
        [FromRoute] Guid connectorId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<TestConnectorConnectionResponse>>(
            new TestConnectorConnectionCommand(connectorId),
            cancellationToken);

        return ToActionResult(result);
    }
}
