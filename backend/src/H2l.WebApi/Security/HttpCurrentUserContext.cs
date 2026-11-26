using System.Security.Claims;
using H2l.Application.Common;
using H2l.Domain.Identity;
using H2l.WebApi.Hubs;

namespace H2l.WebApi.Security;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(TasksHub.TenantIdClaimType);
            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
        }
    }

    public SystemRole? SystemRole
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<SystemRole>(raw, ignoreCase: true, out var role) ? role : null;
        }
    }
}
