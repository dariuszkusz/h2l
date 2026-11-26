using System.Text.Json;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Commands;

public sealed class UpdateFormAccessRulesCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormAccessRuleRepository _formAccessRuleRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateFormAccessRulesCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IFormDefinitionRepository formDefinitionRepository,
        IFormAccessRuleRepository formAccessRuleRepository,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _formDefinitionRepository = formDefinitionRepository;
        _formAccessRuleRepository = formAccessRuleRepository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        UpdateFormAccessRulesCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        var form = await _formDefinitionRepository.GetByIdAsync(command.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure(Error.NotFound("FORM_DEFINITION_NOT_FOUND", "Form definition was not found."));
        }

        var distinctRequests = command.Rules
            .Select(r => (r.GroupId, r.UserId))
            .Distinct()
            .ToArray();

        var referencedGroupIds = distinctRequests
            .Where(r => r.GroupId.HasValue)
            .Select(r => r.GroupId!.Value)
            .Distinct()
            .ToArray();

        var referencedUserIds = distinctRequests
            .Where(r => r.UserId.HasValue)
            .Select(r => r.UserId!.Value)
            .Distinct()
            .ToArray();

        if (referencedGroupIds.Length > 0)
        {
            var groups = await _groupRepository.GetByIdsAsync(referencedGroupIds, cancellationToken);
            if (groups.Count != referencedGroupIds.Length)
            {
                return Result.Failure(Error.Validation("GROUP_NOT_FOUND", "One or more referenced groups do not exist."));
            }
        }

        if (referencedUserIds.Length > 0)
        {
            var users = await _userRepository.GetByIdsAsync(referencedUserIds, cancellationToken);
            if (users.Count != referencedUserIds.Length)
            {
                return Result.Failure(Error.Validation("USER_NOT_FOUND", "One or more referenced users do not exist."));
            }
        }

        var subsetPairs = distinctRequests
            .Where(r => r.GroupId.HasValue && r.UserId.HasValue)
            .Select(r => (GroupId: r.GroupId!.Value, UserId: r.UserId!.Value))
            .ToArray();

        if (subsetPairs.Length > 0)
        {
            var subsetGroupIds = subsetPairs.Select(p => p.GroupId).Distinct().ToArray();
            var subsetUserIds = subsetPairs.Select(p => p.UserId).Distinct().ToArray();
            var existingMemberships = await _groupRepository.GetExistingMembershipsAsync(
                subsetGroupIds,
                subsetUserIds,
                cancellationToken);

            foreach (var pair in subsetPairs)
            {
                if (!existingMemberships.Contains(pair))
                {
                    return Result.Failure(Error.Validation(
                        "USER_NOT_IN_GROUP",
                        "A group subset rule can only reference users who belong to the specified group."));
                }
            }
        }

        var tenantId = _tenantContext.TenantId.Value;
        var newRules = new List<FormAccessRule>(distinctRequests.Length);
        foreach (var (groupId, userId) in distinctRequests)
        {
            var ruleResult = FormAccessRule.Create(tenantId, command.FormId, groupId, userId);
            if (ruleResult.IsFailure)
            {
                return Result.Failure(ruleResult.Error);
            }

            newRules.Add(ruleResult.Value);
        }

        var existingRules = await _formAccessRuleRepository.ListByFormDefinitionIdForUpdateAsync(
            command.FormId,
            cancellationToken);

        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "FORM_ACCESS_RULES_UPDATED",
            "FormDefinition",
            command.FormId,
            JsonSerializer.Serialize(new { ruleCount = newRules.Count }),
            _dateTimeProvider.UtcNow);

        if (auditResult.IsFailure)
        {
            return Result.Failure(auditResult.Error);
        }

        if (existingRules.Count > 0)
        {
            _formAccessRuleRepository.RemoveRange(existingRules);
        }

        if (newRules.Count > 0)
        {
            await _formAccessRuleRepository.AddRangeAsync(newRules, cancellationToken);
        }

        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
