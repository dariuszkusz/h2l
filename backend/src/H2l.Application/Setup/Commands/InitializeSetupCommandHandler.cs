using System.Text.Json;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Setup.Commands;

public sealed class InitializeSetupCommandHandler
{
    private readonly IInstanceSettingsRepository _instanceSettingsRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InitializeSetupCommandHandler(
        IInstanceSettingsRepository instanceSettingsRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        IAesGcmEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _instanceSettingsRepository = instanceSettingsRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<InitializeSetupResponse>> Handle(
        InitializeSetupCommand command,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.AnyAdminExistsAsync(cancellationToken))
        {
            return Result.Failure<InitializeSetupResponse>(
                Error.Conflict(
                    "INSTANCE_ALREADY_INITIALIZED",
                    "The instance has already been initialized."));
        }

        var primarySettings = await _instanceSettingsRepository.GetPrimaryAsync(cancellationToken);
        if (primarySettings is null)
        {
            return Result.Failure<InitializeSetupResponse>(
                Error.NotFound(
                    "INSTANCE_SETTINGS_NOT_FOUND",
                    "Instance settings were not found."));
        }

        var settings = await _instanceSettingsRepository.GetByTenantIdForUpdateAsync(
            primarySettings.TenantId,
            cancellationToken);

        if (settings is null)
        {
            return Result.Failure<InitializeSetupResponse>(
                Error.NotFound(
                    "INSTANCE_SETTINGS_NOT_FOUND",
                    "Instance settings were not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var passwordHash = _passwordHasher.Hash(command.AdminPassword);

        var adminResult = User.Create(
            settings.TenantId,
            command.AdminEmail,
            passwordHash,
            SystemRole.Admin,
            command.DefaultLanguage,
            "light",
            externalId: null,
            createdAt: now);

        if (adminResult.IsFailure)
        {
            return Result.Failure<InitializeSetupResponse>(adminResult.Error);
        }

        string? encryptedSmtp = null;
        if (command.Smtp is not null)
        {
            var smtpConfig = new SmtpConfiguration(
                command.Smtp.Host.Trim(),
                command.Smtp.Port,
                command.Smtp.UseSsl,
                string.IsNullOrWhiteSpace(command.Smtp.Username) ? null : command.Smtp.Username.Trim(),
                command.Smtp.Password,
                command.Smtp.FromAddress.Trim(),
                string.IsNullOrWhiteSpace(command.Smtp.FromName) ? null : command.Smtp.FromName.Trim());

            var smtpJson = JsonSerializer.Serialize(smtpConfig);
            encryptedSmtp = _encryptionService.Encrypt(smtpJson, $"smtp:{settings.TenantId:N}");
        }

        var configResult = settings.UpdateConfiguration(
            command.SelectedPalette,
            command.DefaultLanguage,
            settings.DefaultRetentionDays,
            encryptedSmtp,
            now);

        if (configResult.IsFailure)
        {
            return Result.Failure<InitializeSetupResponse>(configResult.Error);
        }

        var auditMetadata = JsonSerializer.Serialize(new
        {
            selectedPalette = settings.SelectedPalette,
            defaultLanguage = settings.DefaultLanguage,
            smtpConfigured = encryptedSmtp is not null
        });

        var auditResult = AuditLog.Create(
            settings.TenantId,
            adminResult.Value.Id,
            "INSTANCE_INITIALIZED",
            "InstanceSettings",
            settings.Id,
            auditMetadata,
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<InitializeSetupResponse>(auditResult.Error);
        }

        await _userRepository.AddAsync(adminResult.Value, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new InitializeSetupResponse(settings.InstanceId, adminResult.Value.Id));
    }
}
