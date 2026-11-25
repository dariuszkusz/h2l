using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace H2l.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAndSeedAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("H2l.Infrastructure.DatabaseMigration");

        if (!options.AutoMigrate)
        {
            logger.LogInformation("DB_AUTO_MIGRATE is disabled. Skipping automatic EF Core migrations.");
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<H2lDbContext>();
        logger.LogInformation("Applying pending EF Core database migrations...");
        await dbContext.Database.MigrateAsync(cancellationToken);

        await EnsureDefaultSelfHostedInstanceAsync(scope.ServiceProvider, cancellationToken);
    }

    private static async Task EnsureDefaultSelfHostedInstanceAsync(
        IServiceProvider scopedServices,
        CancellationToken cancellationToken)
    {
        var tenantRepository = scopedServices.GetRequiredService<ITenantRepository>();
        var settingsRepository = scopedServices.GetRequiredService<IInstanceSettingsRepository>();
        var unitOfWork = scopedServices.GetRequiredService<IUnitOfWork>();

        if (await tenantRepository.AnyAsync(cancellationToken))
        {
            return;
        }

        var tenantResult = Tenant.Create("Default", "default");
        if (tenantResult.IsFailure)
        {
            throw new InvalidOperationException(tenantResult.Error.Message);
        }

        var settingsResult = InstanceSettings.Create(tenantResult.Value.Id);
        if (settingsResult.IsFailure)
        {
            throw new InvalidOperationException(settingsResult.Error.Message);
        }

        await tenantRepository.AddAsync(tenantResult.Value, cancellationToken);
        await settingsRepository.AddAsync(settingsResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
