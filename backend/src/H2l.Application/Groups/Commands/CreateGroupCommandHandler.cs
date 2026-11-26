using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Groups.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Groups.Commands;

public sealed class CreateGroupCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IGroupRepository _groupRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateGroupCommandHandler(
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

    public async Task<Result<GroupResponse>> Handle(
        CreateGroupCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<GroupResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (await _groupRepository.ExistsByCodeAsync(command.Code, cancellationToken))
        {
            return Result.Failure<GroupResponse>(
                Error.Conflict("GROUP_CODE_ALREADY_EXISTS", "A group with this code already exists."));
        }

        var now = _dateTimeProvider.UtcNow;
        var tenantId = _tenantContext.TenantId.Value;
        var groupResult = Group.Create(tenantId, command.Name, command.Code, now);
        if (groupResult.IsFailure)
        {
            return Result.Failure<GroupResponse>(groupResult.Error);
        }

        var group = groupResult.Value;
        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "GROUP_CREATED",
            "Group",
            group.Id,
            JsonSerializer.Serialize(new { code = group.Code }),
            now);

        if (auditResult.IsFailure)
        {
            return Result.Failure<GroupResponse>(auditResult.Error);
        }

        await _groupRepository.AddAsync(group, cancellationToken);
        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GroupResponse(group.Id, group.Name, group.Code, 0, group.CreatedAt));
    }
}
