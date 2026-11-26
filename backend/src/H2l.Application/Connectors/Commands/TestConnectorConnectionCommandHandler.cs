using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Services;
using H2l.Domain.Common;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Commands;

public sealed class TestConnectorConnectionCommandHandler
{
    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IConnectorConfigSerializer _configSerializer;
    private readonly IWorkflowConnectorRegistry _connectorRegistry;
    private readonly ISsrfGuard _ssrfGuard;

    public TestConnectorConnectionCommandHandler(
        IWorkflowConnectorRepository connectorRepository,
        IConnectorConfigSerializer configSerializer,
        IWorkflowConnectorRegistry connectorRegistry,
        ISsrfGuard ssrfGuard)
    {
        _connectorRepository = connectorRepository;
        _configSerializer = configSerializer;
        _connectorRegistry = connectorRegistry;
        _ssrfGuard = ssrfGuard;
    }

    public async Task<Result<TestConnectorConnectionResponse>> Handle(
        TestConnectorConnectionCommand command,
        CancellationToken cancellationToken)
    {
        var connector = await _connectorRepository.GetByIdAsync(command.ConnectorId, cancellationToken);
        if (connector is null)
        {
            return Result.Failure<TestConnectorConnectionResponse>(
                Error.NotFound("CONNECTOR_NOT_FOUND", "Workflow connector was not found."));
        }

        var ssrfCheck = await _ssrfGuard.ValidateUrlAsync(connector.BaseUrl, cancellationToken);
        if (ssrfCheck.IsFailure)
        {
            return Result.Failure<TestConnectorConnectionResponse>(ssrfCheck.Error);
        }

        var runtimeConfig = _configSerializer.DeserializeRuntimeConfig(connector);
        var implementation = _connectorRegistry.GetConnector(connector.ConnectorType);
        var testResult = await implementation.TestConnectionAsync(runtimeConfig, cancellationToken);

        return Result.Success(new TestConnectorConnectionResponse(
            testResult.IsReachable,
            testResult.DetectedEngine,
            testResult.DetectedVersion,
            testResult.LatencyMs,
            testResult.ErrorMessage));
    }
}
