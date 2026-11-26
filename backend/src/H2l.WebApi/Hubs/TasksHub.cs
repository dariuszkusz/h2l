using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace H2l.WebApi.Hubs;

[Authorize]
public sealed class TasksHub : Hub
{
    public const string HubPath = "/hubs/tasks";
    public const string TenantIdClaimType = "tenant_id";

    public override async Task OnConnectedAsync()
    {
        var tenantIdRaw = Context.User?.FindFirstValue(TenantIdClaimType);
        var userIdRaw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (!Guid.TryParse(tenantIdRaw, out var tenantId) ||
            !Guid.TryParse(userIdRaw, out var userId) ||
            tenantId == Guid.Empty ||
            userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        var userGroupName = BuildUserGroupName(tenantId, userId);
        var tenantGroupName = BuildTenantGroupName(tenantId);
        await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
        await Groups.AddToGroupAsync(Context.ConnectionId, tenantGroupName);
        await base.OnConnectedAsync();
    }

    public static string BuildUserGroupName(Guid tenantId, Guid userId)
        => $"tenant:{tenantId:N}:user:{userId:N}";

    public static string BuildTenantGroupName(Guid tenantId)
        => $"tenant:{tenantId:N}";
}
