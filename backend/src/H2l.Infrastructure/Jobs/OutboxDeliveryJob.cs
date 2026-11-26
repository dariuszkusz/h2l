using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace H2l.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class OutboxDeliveryJob : IJob
{
    public const string TaskSubmittedMessageType = "TASK_SUBMITTED";
    public const string TaskExpiredMessageType = "TASK_EXPIRED";
    public const string TaskCancelledMessageType = "TASK_CANCELLED";

    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IFormSubmissionRepository _formSubmissionRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IWorkflowConnectorRegistry _connectorRegistry;
    private readonly IConnectorConfigSerializer _configSerializer;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ConnectorSecurityOptions _options;
    private readonly ILogger<OutboxDeliveryJob> _logger;

    public OutboxDeliveryJob(
        IOutboxMessageRepository outboxRepository,
        IHumanTaskRepository humanTaskRepository,
        IFormSubmissionRepository formSubmissionRepository,
        IFormVersionRepository formVersionRepository,
        IWorkflowConnectorRepository connectorRepository,
        IAuditLogRepository auditLogRepository,
        IWorkflowConnectorRegistry connectorRegistry,
        IConnectorConfigSerializer configSerializer,
        IAesGcmEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IOptions<ConnectorSecurityOptions> options,
        ILogger<OutboxDeliveryJob> logger)
    {
        _outboxRepository = outboxRepository;
        _humanTaskRepository = humanTaskRepository;
        _formSubmissionRepository = formSubmissionRepository;
        _formVersionRepository = formVersionRepository;
        _connectorRepository = connectorRepository;
        _auditLogRepository = auditLogRepository;
        _connectorRegistry = connectorRegistry;
        _configSerializer = configSerializer;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
        => await ProcessBatchAsync(context.CancellationToken);

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var pendingMessages = await _outboxRepository.ListPendingBatchIgnoringTenantFilterAsync(
            now,
            batchSize: 50,
            cancellationToken);

        var processedCount = 0;
        foreach (var message in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await DeliverSingleMessageAsync(message, cancellationToken);
            processedCount++;
        }

        if (processedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return processedCount;
    }

    private async Task DeliverSingleMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var task = await _humanTaskRepository.GetByIdIgnoringTenantFilterForUpdateAsync(
            message.HumanTaskId,
            cancellationToken);

        if (task is null)
        {
            message.MoveToDeadLetter("Associated human task was not found.", now);
            return;
        }

        if (!task.ConnectorId.HasValue)
        {
            task.ClearResumeUrl();
            message.MarkProcessed(now);
            return;
        }

        var connector = await _connectorRepository.GetByIdIgnoringTenantFilterAsync(
            task.ConnectorId.Value,
            cancellationToken);

        if (connector is null)
        {
            message.MoveToDeadLetter("Associated workflow connector was not found or deleted.", now);
            return;
        }

        ConnectorRuntimeConfig runtimeConfig;
        try
        {
            runtimeConfig = _configSerializer.DeserializeRuntimeConfig(connector);
        }
        catch (Exception)
        {
            message.MoveToDeadLetter("Failed to decrypt workflow connector configuration.", now);
            return;
        }

        string? decryptedResumeUrl = null;
        if (!string.IsNullOrWhiteSpace(task.EncryptedResumeUrl))
        {
            try
            {
                decryptedResumeUrl = _encryptionService.Decrypt(
                    task.EncryptedResumeUrl,
                    $"task-resume:{task.TenantId:N}:{connector.Id:N}");
            }
            catch (Exception)
            {
                message.MoveToDeadLetter("Failed to decrypt task resume URL.", now);
                return;
            }
        }

        var formVersion = await _formVersionRepository.GetByIdIgnoringTenantFilterAsync(
            task.FormVersionId,
            cancellationToken);
        var versionNumber = formVersion?.VersionNumber ?? 1;

        var statusSlug = message.MessageType switch
        {
            TaskExpiredMessageType => "expired",
            TaskCancelledMessageType => "cancelled",
            _ => "completed"
        };

        var taskContext = new ConnectorTaskContext(
            task.TenantId,
            connector.Id,
            task.Id,
            task.ExternalTaskId,
            versionNumber,
            task.SensitiveDataMode,
            decryptedResumeUrl,
            $"h2l:{task.Id:N}:{statusSlug}");

        var implementation = _connectorRegistry.GetConnector(connector.ConnectorType);
        ConnectorDeliveryResult deliveryResult;

        if (string.Equals(message.MessageType, TaskSubmittedMessageType, StringComparison.OrdinalIgnoreCase))
        {
            if (!message.FormSubmissionId.HasValue)
            {
                message.MoveToDeadLetter("Form submission reference is missing on outbox message.", now);
                return;
            }

            var submission = await _formSubmissionRepository.GetByIdIgnoringTenantFilterAsync(
                message.FormSubmissionId.Value,
                cancellationToken);

            if (submission is null)
            {
                message.MoveToDeadLetter("Associated form submission was not found.", now);
                return;
            }

            var publicValues = DeserializeValues(submission.PublicDataJsonb);
            var sensitiveValues = DecryptSensitiveValues(submission);
            var sensitiveKeys = sensitiveValues.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

            var payload = new ConnectorTaskCompletionPayload(
                submission.Id,
                publicValues,
                sensitiveValues,
                sensitiveKeys,
                task.SensitiveDataMode);

            deliveryResult = await implementation.CompleteTaskAsync(
                runtimeConfig,
                taskContext,
                payload,
                cancellationToken);
        }
        else if (string.Equals(message.MessageType, TaskExpiredMessageType, StringComparison.OrdinalIgnoreCase))
        {
            deliveryResult = await implementation.CancelTaskAsync(
                runtimeConfig,
                taskContext,
                "expired",
                "H2L_TASK_EXPIRED",
                cancellationToken);
        }
        else
        {
            deliveryResult = await implementation.CancelTaskAsync(
                runtimeConfig,
                taskContext,
                "cancelled",
                "H2L_TASK_CANCELLED",
                cancellationToken);
        }

        if (deliveryResult.IsSuccess)
        {
            message.MarkProcessed(now);
            task.ClearResumeUrl();
            await RecordAuditAsync(
                task.TenantId,
                "OUTBOX_DELIVERED",
                message.Id,
                JsonSerializer.Serialize(new
                {
                    humanTaskId = task.Id,
                    connectorId = connector.Id,
                    messageType = message.MessageType,
                    httpStatusCode = deliveryResult.HttpStatusCode
                }),
                now,
                cancellationToken);
            return;
        }

        var safeReason = deliveryResult.ErrorMessage ?? "Workflow callback delivery failed.";
        if (deliveryResult.IsTransientError)
        {
            var backoffSeconds = Math.Pow(2, Math.Min(message.RetryCount, 8)) * 15;
            var delay = TimeSpan.FromSeconds(backoffSeconds);
            if (deliveryResult.RetryAfter.HasValue && deliveryResult.RetryAfter.Value > delay)
            {
                delay = deliveryResult.RetryAfter.Value;
            }

            message.ScheduleRetry(Math.Max(1, _options.MaxOutboxRetries), delay, safeReason, now);
            _logger.LogWarning(
                "Transient failure delivering outbox message {OutboxId} (attempt {RetryCount}, status {Status}).",
                message.Id,
                message.RetryCount,
                message.Status);
        }
        else
        {
            message.MoveToDeadLetter(safeReason, now);
            _logger.LogWarning(
                "Permanent failure delivering outbox message {OutboxId}; moved immediately to DeadLetter.",
                message.Id);
        }

        if (message.Status == OutboxMessageStatus.DeadLetter)
        {
            await RecordAuditAsync(
                task.TenantId,
                "OUTBOX_DEAD_LETTERED",
                message.Id,
                JsonSerializer.Serialize(new
                {
                    humanTaskId = task.Id,
                    connectorId = connector.Id,
                    messageType = message.MessageType,
                    httpStatusCode = deliveryResult.HttpStatusCode,
                    retryCount = message.RetryCount
                }),
                now,
                cancellationToken);
        }
    }

    private Dictionary<string, JsonElement> DecryptSensitiveValues(FormSubmission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.EncryptedSensitiveData))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }

        var decrypted = _encryptionService.Decrypt(
            submission.EncryptedSensitiveData,
            $"submission:{submission.TenantId:N}:{submission.Id:N}");

        return DeserializeValues(decrypted);
    }

    private static Dictionary<string, JsonElement> DeserializeValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
    }

    private async Task RecordAuditAsync(
        Guid tenantId,
        string action,
        Guid outboxId,
        string metadataJson,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var auditResult = AuditLog.Create(
            tenantId,
            null,
            action,
            "OutboxMessage",
            outboxId,
            metadataJson,
            now);

        if (auditResult.IsSuccess)
        {
            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        }
    }
}
