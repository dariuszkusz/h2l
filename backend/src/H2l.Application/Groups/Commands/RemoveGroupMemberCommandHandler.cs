using System.Text.Json;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Groups.Commands;

public sealed class RemoveGroupMemberCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IGroupRepository _groupRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveGroupMemberCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IGroupRepository groupRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _groupRepository = groupRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        RemoveGroupMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var group = await _groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group is null)
        {
            return Result.Failure(Error.NotFound("GROUP_NOT_FOUND", "Group was not found."));
        }

        var membership = await _groupRepository.GetMembershipAsync(command.GroupId, command.UserId, cancellationToken);
        if (membership is null)
        {
            return Result.Failure(Error.NotFound("GROUP_MEMBER_NOT_FOUND", "User is not a member of this group."));
        }

        var auditResult = AuditLog.Create(
            _tenantContext.TenantId.Value,
            _currentUserContext.UserId,
            "GROUP_MEMBER_REMOVED",
            "Group",
            group.Id,
            JsonSerializer.Serialize(new { userId = command.UserId }),
            _dateTimeProvider.UtcNow);

        if (auditResult.IsFailure)
        {
            return Result.Failure(auditResult.Error);
        }

        _groupRepository.RemoveMembership(membership);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
