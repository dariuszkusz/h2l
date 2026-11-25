using H2l.Application.Common;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Infrastructure.Health;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Persistence;
using H2l.Infrastructure.Persistence.Repositories;
using H2l.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;

namespace H2l.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .PostConfigure(options =>
            {
                var envConn = configuration["DB_CONNECTION_STRING"]
                    ?? configuration["H2L_DB_CONNECTION_STRING"]
                    ?? configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrWhiteSpace(envConn))
                {
                    options.ConnectionString = envConn;
                }

                var autoMigrateEnv = configuration["DB_AUTO_MIGRATE"];
                if (!string.IsNullOrWhiteSpace(autoMigrateEnv) && bool.TryParse(autoMigrateEnv, out var parsed))
                {
                    options.AutoMigrate = parsed;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(options =>
            {
                var issuer = configuration["JWT_ISSUER"];
                if (!string.IsNullOrWhiteSpace(issuer))
                {
                    options.Issuer = issuer;
                }

                var audience = configuration["JWT_AUDIENCE"];
                if (!string.IsNullOrWhiteSpace(audience))
                {
                    options.Audience = audience;
                }

                var signingKey = configuration["JWT_SIGNING_KEY"];
                if (!string.IsNullOrWhiteSpace(signingKey))
                {
                    options.SigningKey = signingKey;
                }

                var tokenMinutes = configuration["JWT_ACCESS_TOKEN_MINUTES"];
                if (!string.IsNullOrWhiteSpace(tokenMinutes) && int.TryParse(tokenMinutes, out var parsedMinutes))
                {
                    options.AccessTokenLifetimeMinutes = parsedMinutes;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ITenantContext, DefaultTenantContext>();

        services.AddDbContext<H2lDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options
                .UseNpgsql(
                    dbOptions.ConnectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IInstanceSettingsRepository, InstanceSettingsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddQuartz();
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        services.AddHealthChecks()
            .AddDbContextCheck<H2lDbContext>("postgresql", tags: ["ready"])
            .AddCheck<QuartzHealthCheck>("quartz", tags: ["ready"]);

        return services;
    }
}
