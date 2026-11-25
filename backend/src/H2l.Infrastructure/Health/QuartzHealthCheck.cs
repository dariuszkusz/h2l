using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace H2l.Infrastructure.Health;

public sealed class QuartzHealthCheck : IHealthCheck
{
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzHealthCheck(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            if (scheduler.IsShutdown)
            {
                return HealthCheckResult.Unhealthy("Quartz scheduler is shut down.");
            }

            return HealthCheckResult.Healthy("Quartz scheduler is running.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Quartz scheduler check failed.", ex);
        }
    }
}
