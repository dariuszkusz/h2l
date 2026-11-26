using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Licensing;
using H2l.Application.Users.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Users.Commands;

public sealed class CreateUserCommandHandler
{
    private const int ActivationTokenLifetimeHours = 72;

    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserActivationTokenRepository _activationTokenRepository;
    private readonly IInstanceSettingsRepository _instanceSettingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly IOpaqueTokenGenerator _opaqueTokenGenerator;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly ISmtpInviteSender _smtpInviteSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateUserCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IUserActivationTokenRepository activationTokenRepository,
        IInstanceSettingsRepository instanceSettingsRepository,
        IAuditLogRepository auditLogRepository,
        IEntitlementService entitlementService,
        IOpaqueTokenGenerator opaqueTokenGenerator,
        IAesGcmEncryptionService encryptionService,
        ISmtpInviteSender smtpInviteSender,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _activationTokenRepository = activationTokenRepository;
        _instanceSettingsRepository = instanceSettingsRepository;
        _auditLogRepository = auditLogRepository;
        _entitlementService = entitlementService;
        _opaqueTokenGenerator = opaqueTokenGenerator;
        _encryptionService = encryptionService;
        _smtpInviteSender = smtpInviteSender;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<CreateUserResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (command.SystemRole == SystemRole.Admin)
        {
            var entitlementResult = await _entitlementService.CanCreateAdminUserAsync(cancellationToken);
            if (entitlementResult.IsFailure)
            {
                return Result.Failure<CreateUserResponse>(entitlementResult.Error);
            }
        }

        var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result.Failure<CreateUserResponse>(
                Error.Conflict("USER_EMAIL_ALREADY_EXISTS", "A user with this email address already exists."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        var distinctGroupIds = (command.GroupIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        if (distinctGroupIds.Length > 0)
        {
            var existingGroups = await _groupRepository.GetByIdsAsync(distinctGroupIds, cancellationToken);
            if (existingGroups.Count != distinctGroupIds.Length)
            {
                return Result.Failure<CreateUserResponse>(
                    Error.Validation("GROUP_NOT_FOUND", "One or more specified groups do not exist."));
            }
        }

        var settings = await _instanceSettingsRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var defaultLanguage = settings?.DefaultLanguage ?? "en";
        var now = _dateTimeProvider.UtcNow;

        var userResult = User.CreateInvited(
            tenantId,
            command.Email,
            command.SystemRole,
            defaultLanguage,
            "light",
            command.ExternalId,
            now);

        if (userResult.IsFailure)
        {
            return Result.Failure<CreateUserResponse>(userResult.Error);
        }

        var user = userResult.Value;
        var rawToken = _opaqueTokenGenerator.GenerateToken();
        var tokenHash = _opaqueTokenGenerator.ComputeSha256Hash(rawToken);
        var activationTokenResult = UserActivationToken.Create(
            tenantId,
            user.Id,
            tokenHash,
            now.AddHours(ActivationTokenLifetimeHours),
            now);

        if (activationTokenResult.IsFailure)
        {
            return Result.Failure<CreateUserResponse>(activationTokenResult.Error);
        }

        var activationLink = $"/activate?token={Uri.EscapeDataString(rawToken)}";
        var deliveryMethod = "manual_link";
        string? responseActivationLink = activationLink;

        if (settings is not null && !string.IsNullOrWhiteSpace(settings.EncryptedSmtpConfig))
        {
            var smtpJson = _encryptionService.Decrypt(settings.EncryptedSmtpConfig, $"smtp:{tenantId:N}");
            var smtpConfig = JsonSerializer.Deserialize<SmtpConfiguration>(smtpJson);
            if (smtpConfig is not null)
            {
                await _smtpInviteSender.SendActivationInviteAsync(
                    smtpConfig,
                    user.Email,
                    activationLink,
                    defaultLanguage,
                    cancellationToken);

                deliveryMethod = "smtp";
                responseActivationLink = null;
            }
        }

        await _userRepository.AddAsync(user, cancellationToken);
        await _activationTokenRepository.AddAsync(activationTokenResult.Value, cancellationToken);

        foreach (var groupId in distinctGroupIds)
        {
            var membershipResult = UserGroup.Create(tenantId, user.Id, groupId);
            if (membershipResult.IsFailure)
            {
                return Result.Failure<CreateUserResponse>(membershipResult.Error);
            }

            await _groupRepository.AddMembershipAsync(membershipResult.Value, cancellationToken);
        }

        var auditMetadata = JsonSerializer.Serialize(new
        {
            systemRole = user.SystemRole.ToString(),
            deliveryMethod,
            groupCount = distinctGroupIds.Length
        });

        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "USER_INVITED",
            "User",
            user.Id,
            auditMetadata,
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<CreateUserResponse>(auditResult.Error);
        }

        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateUserResponse(
            user.Id,
            user.Email,
            user.ExternalId,
            user.SystemRole,
            user.IsActive,
            deliveryMethod,
            responseActivationLink,
            user.CreatedAt));
    }
}
