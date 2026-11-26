namespace H2l.Application.Common;

public interface IRealtimeTaskNotifier
{
    Task NotifyTaskAssignedAsync(Guid tenantId, Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    Task NotifyTaskUpdatedAsync(Guid tenantId, Guid taskId, string eventType, CancellationToken cancellationToken = default);
}
