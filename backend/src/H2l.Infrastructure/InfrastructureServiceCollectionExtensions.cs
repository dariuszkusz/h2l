using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Licensing;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Connectors;
using H2l.Infrastructure.Connectors.Security;
using H2l.Infrastructure.Email;
using H2l.Infrastructure.Health;
using H2l.Infrastructure.Jobs;
using H2l.Infrastructure.Licensing;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Persistence;
using H2l.Infrastructure.Persistence.Repositories;
using H2l.Infrastructure.Security;
using H2l.Infrastructure.Storage;
using H2l.Infrastructure.Tenancy;
using H2l.Infrastructure.Time;
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

                var refreshDays = configuration["JWT_REFRESH_TOKEN_DAYS"];
                if (!string.IsNullOrWhiteSpace(refreshDays) && int.TryParse(refreshDays, out var parsedDays))
                {
                    options.RefreshTokenLifetimeDays = parsedDays;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<EncryptionOptions>, EncryptionOptionsValidator>();
        services.AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .PostConfigure(options =>
            {
                var keyId = configuration["ENCRYPTION_KEY_ID"];
                if (!string.IsNullOrWhiteSpace(keyId))
                {
                    options.KeyId = keyId;
                }

                var aesKey = configuration["ENCRYPTION_AES_GCM_KEY"];
                if (!string.IsNullOrWhiteSpace(aesKey))
                {
                    options.AesGcmKey = aesKey;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LicensingOptions>()
            .Bind(configuration.GetSection(LicensingOptions.SectionName))
            .PostConfigure(options =>
            {
                var publicKey = configuration["LICENSE_PUBLIC_KEY_BASE64"];
                if (!string.IsNullOrWhiteSpace(publicKey))
                {
                    options.PublicKeyBase64 = publicKey;
                }
            });

        services.AddOptions<AttachmentsOptions>()
            .Bind(configuration.GetSection(AttachmentsOptions.SectionName))
            .PostConfigure(options =>
            {
                var maxSizeMb = configuration["ATTACHMENTS_MAX_SIZE_MB"];
                if (!string.IsNullOrWhiteSpace(maxSizeMb) && int.TryParse(maxSizeMb, out var parsedMb))
                {
                    options.MaxSizeMb = parsedMb;
                }

                var storagePath = configuration["ATTACHMENTS_STORAGE_PATH"];
                if (!string.IsNullOrWhiteSpace(storagePath))
                {
                    options.StoragePath = storagePath;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectorOptions = new ConnectorSecurityOptions();
        configuration.GetSection(ConnectorSecurityOptions.SectionName).Bind(connectorOptions);
        ApplyConnectorEnvironmentOverrides(configuration, connectorOptions);

        services.AddOptions<ConnectorSecurityOptions>()
            .Bind(configuration.GetSection(ConnectorSecurityOptions.SectionName))
            .PostConfigure(options => ApplyConnectorEnvironmentOverrides(configuration, options));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IAesGcmEncryptionService, AesGcmEncryptionService>();
        services.AddSingleton<IAttachmentStorageService, EncryptedLocalAttachmentStorageService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ISmtpInviteSender, SmtpInviteSender>();
        services.AddSingleton<ILicensePublicKeyProvider, ConfiguredLicensePublicKeyProvider>();
        services.AddSingleton<ILicenseVerifier, Ed25519LicenseVerifier>();
        services.AddScoped<IEntitlementService, SelfHostedEntitlementService>();

        services.AddSingleton<IDnsResolver, SystemDnsResolver>();
        services.AddSingleton<ISsrfGuard, SsrfGuard>();
        services.AddSingleton<IHmacWebhookSigner, HmacWebhookSigner>();
        services.AddSingleton<IConnectorHttpClientFactory, ConnectorHttpClientFactory>();
        services.AddSingleton<ITokenProvider, ConnectorTokenProvider>();

        services.AddSingleton<UniversalHttpWorkflowConnector>();
        services.AddSingleton<N8nWorkflowConnector>();
        services.AddSingleton<Camunda8WorkflowConnector>();
        services.AddSingleton<IWorkflowConnector>(sp => sp.GetRequiredService<UniversalHttpWorkflowConnector>());
        services.AddSingleton<IWorkflowConnector>(sp => sp.GetRequiredService<N8nWorkflowConnector>());
        services.AddSingleton<IWorkflowConnector>(sp => sp.GetRequiredService<Camunda8WorkflowConnector>());
        services.AddSingleton<IWorkflowConnectorRegistry, WorkflowConnectorRegistry>();

        if (connectorOptions.EnableCamundaPullWorker)
        {
            services.AddHostedService<Camunda8PullWorkerBackgroundService>();
        }

        services.AddScoped<DefaultTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<DefaultTenantContext>());
        services.AddScoped<ITenantSetter>(sp => sp.GetRequiredService<DefaultTenantContext>());

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
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IUserActivationTokenRepository, UserActivationTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorkflowConnectorRepository, WorkflowConnectorRepository>();
        services.AddScoped<IWorkflowProcessRepository, WorkflowProcessRepository>();
        services.AddScoped<IFormDefinitionRepository, FormDefinitionRepository>();
        services.AddScoped<IFormVersionRepository, FormVersionRepository>();
        services.AddScoped<IFormAccessRuleRepository, FormAccessRuleRepository>();
        services.AddScoped<IHumanTaskRepository, HumanTaskRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        services.AddScoped<ISubmissionAttachmentRepository, SubmissionAttachmentRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

        var quartzConnectionString = configuration["DB_CONNECTION_STRING"]
            ?? configuration["H2L_DB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["Database:ConnectionString"];

        services.AddQuartz(q =>
        {
            q.SchedulerId = "AUTO";
            q.SchedulerName = "h2l-scheduler";

            if (connectorOptions.UsePersistentQuartzStore && !string.IsNullOrWhiteSpace(quartzConnectionString))
            {
                q.UsePersistentStore(store =>
                {
                    store.UseProperties = true;
                    store.UsePostgres(pg =>
                    {
                        pg.ConnectionString = quartzConnectionString;
                        pg.TablePrefix = "quartz.qrtz_";
                    });
                    store.UseSystemTextJsonSerializer();
                    store.UseClustering();
                });
            }

            var outboxJobKey = new JobKey("outbox-delivery-job", "h2l");
            q.AddJob<OutboxDeliveryJob>(opts => opts.WithIdentity(outboxJobKey));
            q.AddTrigger(opts => opts
                .ForJob(outboxJobKey)
                .WithIdentity("outbox-delivery-trigger", "h2l")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(Math.Max(5, connectorOptions.OutboxPollIntervalSeconds))
                    .RepeatForever()));

            var expirationJobKey = new JobKey("task-expiration-job", "h2l");
            q.AddJob<TaskExpirationJob>(opts => opts.WithIdentity(expirationJobKey));
            q.AddTrigger(opts => opts
                .ForJob(expirationJobKey)
                .WithIdentity("task-expiration-trigger", "h2l")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(Math.Max(15, connectorOptions.TaskExpirationPollIntervalSeconds))
                    .RepeatForever()));

            var retentionJobKey = new JobKey("data-retention-job", "h2l");
            q.AddJob<DataRetentionJob>(opts => opts.WithIdentity(retentionJobKey));
            q.AddTrigger(opts => opts
                .ForJob(retentionJobKey)
                .WithIdentity("data-retention-trigger", "h2l")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(Math.Max(5, connectorOptions.DataRetentionPollIntervalMinutes))
                    .RepeatForever()));

            var lockExtendJobKey = new JobKey("camunda-lock-extension-job", "h2l");
            q.AddJob<CamundaLockExtensionJob>(opts => opts.WithIdentity(lockExtendJobKey));
            q.AddTrigger(opts => opts
                .ForJob(lockExtendJobKey)
                .WithIdentity("camunda-lock-extension-trigger", "h2l")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(Math.Max(1, connectorOptions.CamundaLockExtendIntervalMinutes))
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        services.AddHealthChecks()
            .AddDbContextCheck<H2lDbContext>("postgresql", tags: ["ready"])
            .AddCheck<QuartzHealthCheck>("quartz", tags: ["ready"]);

        return services;
    }

    private static void ApplyConnectorEnvironmentOverrides(
        IConfiguration configuration,
        ConnectorSecurityOptions options)
    {
        var allowPrivate = configuration["CONNECTOR_ALLOW_PRIVATE_NETWORKS"];
        if (!string.IsNullOrWhiteSpace(allowPrivate) && bool.TryParse(allowPrivate, out var parsedAllow))
        {
            options.AllowPrivateNetworks = parsedAllow;
        }

        var allowedHosts = configuration["CONNECTOR_ALLOWED_HOSTS"];
        if (!string.IsNullOrWhiteSpace(allowedHosts))
        {
            options.AllowedHosts = allowedHosts
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var persistentQuartz = configuration["QUARTZ_USE_PERSISTENT_STORE"];
        if (!string.IsNullOrWhiteSpace(persistentQuartz) && bool.TryParse(persistentQuartz, out var parsedQuartz))
        {
            options.UsePersistentQuartzStore = parsedQuartz;
        }

        var enableCamundaWorker = configuration["CAMUNDA_PULL_WORKER_ENABLED"];
        if (!string.IsNullOrWhiteSpace(enableCamundaWorker) && bool.TryParse(enableCamundaWorker, out var parsedCamunda))
        {
            options.EnableCamundaPullWorker = parsedCamunda;
        }
    }
}
