using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using H2l.Domain.Licensing;
using H2l.Domain.Workflows;

namespace H2l.Infrastructure.Licensing;

public sealed class SelfHostedEntitlementService : IEntitlementService
{
    private static readonly LicenseLimits FreeLimits = new(
        MaxActiveProcesses: 1,
        MaxActiveConnectors: 1,
        MaxAdminUsers: 1,
        MaxEndUsers: null);

    private readonly ITenantContext _tenantContext;
    private readonly IInstanceSettingsRepository _instanceSettingsRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWorkflowProcessRepository _workflowProcessRepository;
    private readonly IWorkflowConnectorRepository _workflowConnectorRepository;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly ILicenseVerifier _licenseVerifier;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SelfHostedEntitlementService(
        ITenantContext tenantContext,
        IInstanceSettingsRepository instanceSettingsRepository,
        IUserRepository userRepository,
        IWorkflowProcessRepository workflowProcessRepository,
        IWorkflowConnectorRepository workflowConnectorRepository,
        IAesGcmEncryptionService encryptionService,
        ILicenseVerifier licenseVerifier,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _instanceSettingsRepository = instanceSettingsRepository;
        _userRepository = userRepository;
        _workflowProcessRepository = workflowProcessRepository;
        _workflowConnectorRepository = workflowConnectorRepository;
        _encryptionService = encryptionService;
        _licenseVerifier = licenseVerifier;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LicensingStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        InstanceSettings? settings = null;
        if (_tenantContext.TenantId is not null && _tenantContext.TenantId != Guid.Empty)
        {
            settings = await _instanceSettingsRepository.GetByTenantIdAsync(
                _tenantContext.TenantId.Value,
                cancellationToken);
        }

        settings ??= await _instanceSettingsRepository.GetPrimaryAsync(cancellationToken);
        if (settings is null)
        {
            return Result.Failure<LicensingStatusResponse>(
                Error.NotFound("INSTANCE_SETTINGS_NOT_FOUND", "Instance settings were not found."));
        }

        var activeProcesses = await _workflowProcessRepository.CountActiveAsync(cancellationToken);
        var activeConnectors = await _workflowConnectorRepository.CountActiveAsync(cancellationToken);
        var adminUsers = await _userRepository.CountActiveAdminsAsync(cancellationToken);
        var endUsers = await _userRepository.CountActiveEndUsersAsync(cancellationToken);

        var usage = new LicenseUsage(
            ActiveProcesses: activeProcesses,
            ActiveConnectors: activeConnectors,
            AdminUsers: adminUsers,
            EndUsers: endUsers);

        if (string.IsNullOrWhiteSpace(settings.EncryptedLicenseKey))
        {
            return Result.Success(new LicensingStatusResponse(
                InstanceId: settings.InstanceId,
                Plan: LicensePlan.Free,
                Status: LicenseStatus.Active,
                ExpiresAt: null,
                GracePeriodEndsAt: null,
                InGracePeriod: false,
                Usage: usage,
                Limits: FreeLimits));
        }

        string decryptedLicenseKey;
        try
        {
            decryptedLicenseKey = _encryptionService.Decrypt(
                settings.EncryptedLicenseKey,
                $"license:{settings.TenantId:N}");
        }
        catch
        {
            return Result.Success(new LicensingStatusResponse(
                InstanceId: settings.InstanceId,
                Plan: LicensePlan.Free,
                Status: LicenseStatus.Active,
                ExpiresAt: null,
                GracePeriodEndsAt: null,
                InGracePeriod: false,
                Usage: usage,
                Limits: FreeLimits));
        }

        var verificationResult = _licenseVerifier.Verify(
            decryptedLicenseKey,
            settings.InstanceId,
            _dateTimeProvider.UtcNow);

        if (verificationResult.IsFailure)
        {
            return Result.Success(new LicensingStatusResponse(
                InstanceId: settings.InstanceId,
                Plan: LicensePlan.Free,
                Status: LicenseStatus.Active,
                ExpiresAt: null,
                GracePeriodEndsAt: null,
                InGracePeriod: false,
                Usage: usage,
                Limits: FreeLimits));
        }

        var verified = verificationResult.Value;
        var effectiveLimits = verified.Status == LicenseStatus.Expired
            ? FreeLimits
            : new LicenseLimits(
                MaxActiveProcesses: verified.Payload.MaxActiveProcesses,
                MaxActiveConnectors: verified.Payload.MaxActiveConnectors,
                MaxAdminUsers: verified.Payload.MaxAdminUsers,
                MaxEndUsers: verified.Payload.MaxEndUsers);

        return Result.Success(new LicensingStatusResponse(
            InstanceId: settings.InstanceId,
            Plan: verified.Payload.Plan,
            Status: verified.Status,
            ExpiresAt: verified.Payload.ExpiresAt,
            GracePeriodEndsAt: verified.GracePeriodEndsAt,
            InGracePeriod: verified.InGracePeriod,
            Usage: usage,
            Limits: effectiveLimits));
    }

    public async Task<Result> CanCreateAdminUserAsync(CancellationToken cancellationToken = default)
    {
        var statusResult = await GetStatusAsync(cancellationToken);
        if (statusResult.IsFailure)
        {
            return Result.Failure(statusResult.Error);
        }

        var status = statusResult.Value;
        if (status.Limits.MaxAdminUsers.HasValue &&
            status.Usage.AdminUsers >= status.Limits.MaxAdminUsers.Value)
        {
            return Result.Failure(Error.PaymentRequired(
                "ADMIN_LIMIT_EXCEEDED",
                $"Current plan ({status.Plan}) allows at most {status.Limits.MaxAdminUsers.Value} active admin user(s). Upgrade your license to add more admins."));
        }

        return Result.Success();
    }

    public async Task<Result> CanActivateWorkflowProcessAsync(CancellationToken cancellationToken = default)
    {
        var statusResult = await GetStatusAsync(cancellationToken);
        if (statusResult.IsFailure)
        {
            return Result.Failure(statusResult.Error);
        }

        var status = statusResult.Value;
        if (status.Limits.MaxActiveProcesses.HasValue &&
            status.Usage.ActiveProcesses >= status.Limits.MaxActiveProcesses.Value)
        {
            return Result.Failure(Error.PaymentRequired(
                "WORKFLOW_PROCESS_LIMIT_EXCEEDED",
                $"Current plan ({status.Plan}) allows at most {status.Limits.MaxActiveProcesses.Value} active workflow process(es). Upgrade your license to activate more processes."));
        }

        return Result.Success();
    }

    public async Task<Result> CanActivateConnectorAsync(CancellationToken cancellationToken = default)
    {
        var statusResult = await GetStatusAsync(cancellationToken);
        if (statusResult.IsFailure)
        {
            return Result.Failure(statusResult.Error);
        }

        var status = statusResult.Value;
        if (status.Limits.MaxActiveConnectors.HasValue &&
            status.Usage.ActiveConnectors >= status.Limits.MaxActiveConnectors.Value)
        {
            return Result.Failure(Error.PaymentRequired(
                "CONNECTOR_LIMIT_EXCEEDED",
                $"Current plan ({status.Plan}) allows at most {status.Limits.MaxActiveConnectors.Value} active workflow connector(s). Upgrade your license to add more connectors."));
        }

        return Result.Success();
    }
}
