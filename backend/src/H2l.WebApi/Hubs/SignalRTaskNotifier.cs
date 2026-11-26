using H2l.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace H2l.WebApi.Hubs;

public sealed class SignalRTaskNotifier : IRealtimeTaskNotifier
{
    private readonly IHubContext<TasksHub> _hubContext;

    public SignalRTaskNotifier(IHubContext<TasksHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyTaskAssignedAsync(
        Guid tenantId,
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var groupName = TasksHub.BuildUserGroupName(tenantId, userId);
        return _hubContext.Clients
            .Group(groupName)
            .SendAsync("TaskAssigned", new { taskId }, cancellationToken);
    }

    public Task NotifyTaskUpdatedAsync(
        Guid tenantId,
        Guid taskId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var groupName = TasksHub.BuildTenantGroupName(tenantId);
        return _hubContext.Clients
            .Group(groupName)
            .SendAsync("TaskUpdated", new { taskId, eventType }, cancellationToken);
    }
}
