using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Groups.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Groups.Commands;

public sealed class AddGroupMemberCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddGroupMemberCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<GroupMemberResponse>> Handle(
        AddGroupMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<GroupMemberResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var group = await _groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<GroupMemberResponse>(
                Error.NotFound("GROUP_NOT_FOUND", "Group was not found."));
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<GroupMemberResponse>(
                Error.NotFound("USER_NOT_FOUND", "User was not found."));
        }

        var existing = await _groupRepository.GetMembershipAsync(command.GroupId, command.UserId, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<GroupMemberResponse>(
                Error.Conflict("GROUP_MEMBER_ALREADY_EXISTS", "User is already a member of this group."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        var membershipResult = UserGroup.Create(tenantId, command.UserId, command.GroupId);
        if (membershipResult.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(membershipResult.Error);
        }

        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "GROUP_MEMBER_ADDED",
            "Group",
            group.Id,
            JsonSerializer.Serialize(new { userId = user.Id }),
            _dateTimeProvider.UtcNow);

        if (auditResult.IsFailure)
        {
            return Result.Failure<GroupMemberResponse>(auditResult.Error);
        }

        await _groupRepository.AddMembershipAsync(membershipResult.Value, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GroupMemberResponse(
            user.Id,
            user.Email,
            user.ExternalId,
            user.SystemRole,
            user.IsActive));
    }
}
