using System.Security.Claims;
using H2l.Application.Common;
using H2l.Domain.Identity;
using H2l.WebApi.Hubs;
using Microsoft.AspNetCore.Mvc;

namespace H2l.WebApi.Middleware;

public sealed class TenantResolutionMiddleware
{
    private const string TenantSlugHeader = "X-Tenant-Slug";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantSetter tenantSetter,
        ITenantRepository tenantRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdRaw = context.User.FindFirstValue(TasksHub.TenantIdClaimType);
            if (!Guid.TryParse(tenantIdRaw, out var tenantId) || tenantId == Guid.Empty)
            {
                await WriteUnauthorizedProblemAsync(context);
                return;
            }

            tenantSetter.SetTenantId(tenantId);
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/v1/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
        {
            Tenant? tenant = null;
            if (context.Request.Headers.TryGetValue(TenantSlugHeader, out var slugValues))
            {
                var slug = slugValues.ToString().Trim();
                if (!string.IsNullOrEmpty(slug))
                {
                    tenant = await tenantRepository.GetBySlugAsync(slug, context.RequestAborted);
                }
            }

            tenant ??= await tenantRepository.GetPrimaryAsync(context.RequestAborted);

            if (tenant is not null && tenant.IsActive)
            {
                tenantSetter.SetTenantId(tenant.Id);
            }
        }

        await _next(context);
    }

    private static async Task WriteUnauthorizedProblemAsync(HttpContext context)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authenticated token is missing a valid tenant identifier.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["errorCode"] = "TENANT_CLAIM_INVALID";

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
