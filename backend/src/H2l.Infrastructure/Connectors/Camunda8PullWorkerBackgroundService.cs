using H2l.Application.Common;
using H2l.Application.Connectors.Commands;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Services;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Tasks;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;

namespace H2l.Infrastructure.Connectors;

public sealed class Camunda8PullWorkerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Camunda8WorkflowConnector _camundaConnector;
    private readonly IConnectorConfigSerializer _configSerializer;
    private readonly ConnectorSecurityOptions _options;
    private readonly ILogger<Camunda8PullWorkerBackgroundService> _logger;

    public Camunda8PullWorkerBackgroundService(
        IServiceScopeFactory scopeFactory,
        Camunda8WorkflowConnector camundaConnector,
        IConnectorConfigSerializer configSerializer,
        IOptions<ConnectorSecurityOptions> options,
        ILogger<Camunda8PullWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _camundaConnector = camundaConnector;
        _configSerializer = configSerializer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableCamundaPullWorker)
        {
            return;
        }

        var pollDelay = TimeSpan.FromSeconds(Math.Max(2, _options.CamundaPollIntervalSeconds));
        var lockTimeout = TimeSpan.FromMinutes(Math.Max(10, _options.CamundaLockExtendIntervalMinutes * 2));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllActiveCamundaConnectorsAsync(lockTimeout, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Camunda 8 pull worker loop encountered an unexpected error.");
            }

            try
            {
                await Task.Delay(pollDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PollAllActiveCamundaConnectorsAsync(TimeSpan lockTimeout, CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkflowConnector> activeConnectors;
        await using (var discoveryScope = _scopeFactory.CreateAsyncScope())
        {
            var connectorRepository = discoveryScope.ServiceProvider.GetRequiredService<IWorkflowConnectorRepository>();
            activeConnectors = await connectorRepository.ListActiveByTypeIgnoringTenantFilterAsync(
                ConnectorType.Camunda8,
                cancellationToken);
        }

        foreach (var connector in activeConnectors)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await PollSingleConnectorAsync(connector, lockTimeout, cancellationToken);
        }
    }

    private async Task PollSingleConnectorAsync(
        WorkflowConnector connector,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken)
    {
        ConnectorRuntimeConfig runtimeConfig;
        try
        {
            runtimeConfig = _configSerializer.DeserializeRuntimeConfig(connector);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt config for Camunda 8 connector {ConnectorId}.", connector.Id);
            return;
        }

        IReadOnlyList<Camunda8ActivatedJob> jobs;
        try
        {
            jobs = await _camundaConnector.ActivateJobsAsync(runtimeConfig, 10, lockTimeout, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Camunda 8 job activation failed for connector {ConnectorId}.", connector.Id);
            return;
        }

        foreach (var job in jobs)
        {
            await ProcessActivatedJobAsync(runtimeConfig, job, cancellationToken);
        }
    }

    private async Task ProcessActivatedJobAsync(
        ConnectorRuntimeConfig config,
        Camunda8ActivatedJob job,
        CancellationToken cancellationToken)
    {
        if (!job.CustomHeaders.TryGetValue("formId", out var rawFormId) ||
            !Guid.TryParse(rawFormId, out var formId))
        {
            await ThrowBpmnErrorAsync(
                config,
                job.JobKey,
                Camunda8WorkflowConnector.ErrorFormNotFound,
                "Missing or invalid 'formId' custom header on h2l-human-task.",
                cancellationToken);
            return;
        }

        int? formVersion = null;
        if (job.CustomHeaders.TryGetValue("formVersion", out var rawVersion) &&
            int.TryParse(rawVersion, out var parsedVersion) &&
            parsedVersion >= 1)
        {
            formVersion = parsedVersion;
        }

        job.CustomHeaders.TryGetValue("assignee", out var assignee);
        job.CustomHeaders.TryGetValue("timeout", out var timeoutIso8601);

        List<string>? candidateGroups = null;
        if (job.CustomHeaders.TryGetValue("candidateGroups", out var rawGroups) &&
            !string.IsNullOrWhiteSpace(rawGroups))
        {
            candidateGroups = rawGroups
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        var sensitiveMode = SensitiveDataMode.ClaimCheck;
        if (job.CustomHeaders.TryGetValue("sensitiveDataMode", out var rawMode) &&
            Enum.TryParse<SensitiveDataMode>(rawMode, ignoreCase: true, out var parsedMode))
        {
            sensitiveMode = parsedMode;
        }

        var throwOnMissingAssignee = job.CustomHeaders.TryGetValue("throwOnMissingAssignee", out var rawThrow)
            && bool.TryParse(rawThrow, out var parsedThrow)
            && parsedThrow;

        await using var tenantScope = _scopeFactory.CreateAsyncScope();
        var tenantSetter = tenantScope.ServiceProvider.GetRequiredService<ITenantSetter>();
        tenantSetter.SetTenantId(config.TenantId);

        var messageBus = tenantScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var command = new CreateInboundHumanTaskCommand(
            config.ConnectorId,
            job.JobKey,
            formId,
            formVersion,
            string.IsNullOrWhiteSpace(assignee) ? null : assignee,
            candidateGroups,
            job.Variables.Count > 0 ? job.Variables : null,
            ResumeUrl: null,
            string.IsNullOrWhiteSpace(timeoutIso8601) ? null : timeoutIso8601,
            sensitiveMode,
            throwOnMissingAssignee);

        var result = await messageBus.InvokeAsync<Result<InboundWorkflowTaskResponse>>(command, cancellationToken);
        if (result.IsFailure &&
            (result.Error.Code == Camunda8WorkflowConnector.ErrorFormNotFound
             || result.Error.Code == Camunda8WorkflowConnector.ErrorAssigneeNotFound))
        {
            await ThrowBpmnErrorAsync(config, job.JobKey, result.Error.Code, result.Error.Message, cancellationToken);
        }
    }

    private async Task ThrowBpmnErrorAsync(
        ConnectorRuntimeConfig config,
        string jobKey,
        string errorCode,
        string reason,
        CancellationToken cancellationToken)
    {
        var context = new ConnectorTaskContext(
            config.TenantId,
            config.ConnectorId,
            Guid.Empty,
            jobKey,
            0,
            SensitiveDataMode.ClaimCheck,
            null,
            $"h2l:{jobKey}:error");

        await _camundaConnector.CancelTaskAsync(config, context, "error", errorCode, cancellationToken);
    }
}
