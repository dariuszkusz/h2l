using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Domain.Connectors;
using H2l.Domain.Tasks;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace H2l.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class CamundaLockExtensionJob : IJob
{
    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IWorkflowConnectorRegistry _connectorRegistry;
    private readonly IConnectorConfigSerializer _configSerializer;
    private readonly ConnectorSecurityOptions _options;
    private readonly ILogger<CamundaLockExtensionJob> _logger;

    public CamundaLockExtensionJob(
        IWorkflowConnectorRepository connectorRepository,
        IHumanTaskRepository humanTaskRepository,
        IWorkflowConnectorRegistry connectorRegistry,
        IConnectorConfigSerializer configSerializer,
        IOptions<ConnectorSecurityOptions> options,
        ILogger<CamundaLockExtensionJob> logger)
    {
        _connectorRepository = connectorRepository;
        _humanTaskRepository = humanTaskRepository;
        _connectorRegistry = connectorRegistry;
        _configSerializer = configSerializer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
        => await ProcessBatchAsync(context.CancellationToken);

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var pullConnector = _connectorRegistry.TryGetPullConnector(ConnectorType.Camunda8);
        if (pullConnector is null)
        {
            return 0;
        }

        var activeConnectors = await _connectorRepository.ListActiveByTypeIgnoringTenantFilterAsync(
            ConnectorType.Camunda8,
            cancellationToken);

        var extensionDuration = TimeSpan.FromMinutes(Math.Max(10, _options.CamundaLockExtendIntervalMinutes * 2));
        var extendedCount = 0;

        foreach (var connector in activeConnectors)
        {
            ConnectorRuntimeConfig runtimeConfig;
            try
            {
                runtimeConfig = _configSerializer.DeserializeRuntimeConfig(connector);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt Camunda 8 config for lock extension on connector {ConnectorId}.", connector.Id);
                continue;
            }

            var activeTasks = await _humanTaskRepository.ListActiveTasksByConnectorIgnoringTenantFilterAsync(
                connector.Id,
                batchSize: 100,
                cancellationToken);

            foreach (var task in activeTasks)
            {
                var taskContext = new ConnectorTaskContext(
                    task.TenantId,
                    connector.Id,
                    task.Id,
                    task.ExternalTaskId,
                    0,
                    task.SensitiveDataMode,
                    null,
                    $"h2l:{task.Id:N}:extend-lock");

                var result = await pullConnector.ExtendLockAsync(
                    runtimeConfig,
                    taskContext,
                    extensionDuration,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    extendedCount++;
                }
            }
        }

        return extendedCount;
    }
}
