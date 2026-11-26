using System.Text.Json;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Licensing.Commands;

public sealed class ActivateLicenseCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IInstanceSettingsRepository _instanceSettingsRepository;
    private readonly ILicenseVerifier _licenseVerifier;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntitlementService _entitlementService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ActivateLicenseCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IInstanceSettingsRepository instanceSettingsRepository,
        ILicenseVerifier licenseVerifier,
        IAesGcmEncryptionService encryptionService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IEntitlementService entitlementService,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _instanceSettingsRepository = instanceSettingsRepository;
        _licenseVerifier = licenseVerifier;
        _encryptionService = encryptionService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _entitlementService = entitlementService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LicensingStatusResponse>> Handle(
        ActivateLicenseCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<LicensingStatusResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        var settings = await _instanceSettingsRepository.GetByTenantIdForUpdateAsync(tenantId, cancellationToken);
        if (settings is null)
        {
            return Result.Failure<LicensingStatusResponse>(
                Error.NotFound("INSTANCE_SETTINGS_NOT_FOUND", "Instance settings were not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var verifiedResult = _licenseVerifier.Verify(command.LicenseKey, settings.InstanceId, now);
        if (verifiedResult.IsFailure)
        {
            return Result.Failure<LicensingStatusResponse>(verifiedResult.Error);
        }

        var encryptedKey = _encryptionService.Encrypt(command.LicenseKey.Trim(), $"license:{tenantId:N}");
        settings.SetEncryptedLicenseKey(encryptedKey, now);

        var metadata = JsonSerializer.Serialize(new
        {
            licenseId = verifiedResult.Value.Payload.LicenseId,
            plan = verifiedResult.Value.Payload.Plan.ToString(),
            status = verifiedResult.Value.Status.ToString()
        });

        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "LICENSE_ACTIVATED",
            "InstanceSettings",
            settings.Id,
            metadata,
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<LicensingStatusResponse>(auditResult.Error);
        }

        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _entitlementService.GetStatusAsync(cancellationToken);
    }
}
