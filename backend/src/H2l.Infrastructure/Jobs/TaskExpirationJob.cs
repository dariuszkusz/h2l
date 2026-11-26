using System.Text.Json;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using Quartz;

namespace H2l.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class TaskExpirationJob : IJob
{
    private readonly IHumanTaskRepository _humanTaskRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeTaskNotifier _realtimeTaskNotifier;

    public TaskExpirationJob(
        IHumanTaskRepository humanTaskRepository,
        IOutboxMessageRepository outboxMessageRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRealtimeTaskNotifier realtimeTaskNotifier)
    {
        _humanTaskRepository = humanTaskRepository;
        _outboxMessageRepository = outboxMessageRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _realtimeTaskNotifier = realtimeTaskNotifier;
    }

    public async Task Execute(IJobExecutionContext context)
        => await ProcessBatchAsync(context.CancellationToken);

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var overdueTasks = await _humanTaskRepository.ListExpiredOpenTasksIgnoringTenantFilterAsync(
            now,
            batchSize: 100,
            cancellationToken);

        if (overdueTasks.Count == 0)
        {
            return 0;
        }

        var expiredTasks = new List<HumanTask>(overdueTasks.Count);
        foreach (var task in overdueTasks)
        {
            var expireResult = task.Expire(now, clearResumeUrl: !task.ConnectorId.HasValue);
            if (expireResult.IsFailure)
            {
                continue;
            }

            if (task.ConnectorId.HasValue)
            {
                var outboxResult = OutboxMessage.Create(
                    task.TenantId,
                    task.Id,
                    null,
                    OutboxDeliveryJob.TaskExpiredMessageType,
                    now);

                if (outboxResult.IsSuccess)
                {
                    await _outboxMessageRepository.AddAsync(outboxResult.Value, cancellationToken);
                }
            }

            var auditResult = AuditLog.Create(
                task.TenantId,
                null,
                "TASK_EXPIRED",
                "HumanTask",
                task.Id,
                JsonSerializer.Serialize(new { connectorId = task.ConnectorId }),
                now);

            if (auditResult.IsSuccess)
            {
                await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
            }

            expiredTasks.Add(task);
        }

        if (expiredTasks.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var task in expiredTasks)
            {
                await _realtimeTaskNotifier.NotifyTaskUpdatedAsync(
                    task.TenantId,
                    task.Id,
                    "TaskExpired",
                    cancellationToken);
            }
        }

        return expiredTasks.Count;
    }
}
