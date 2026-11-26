using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Email;
using H2l.Infrastructure.Health;
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
        services.AddScoped<IWorkflowProcessRepository, WorkflowProcessRepository>();
        services.AddScoped<IFormDefinitionRepository, FormDefinitionRepository>();
        services.AddScoped<IFormVersionRepository, FormVersionRepository>();
        services.AddScoped<IFormAccessRuleRepository, FormAccessRuleRepository>();
        services.AddScoped<IHumanTaskRepository, HumanTaskRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        services.AddScoped<ISubmissionAttachmentRepository, SubmissionAttachmentRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

        services.AddQuartz();
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        services.AddHealthChecks()
            .AddDbContextCheck<H2lDbContext>("postgresql", tags: ["ready"])
            .AddCheck<QuartzHealthCheck>("quartz", tags: ["ready"]);

        return services;
    }
}
