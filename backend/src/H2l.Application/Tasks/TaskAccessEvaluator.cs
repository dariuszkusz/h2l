using H2l.Domain.Identity;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;

namespace H2l.Application.Tasks;

public sealed class TaskAccessEvaluator : ITaskAccessEvaluator
{
    private readonly IFormAccessRuleRepository _formAccessRuleRepository;
    private readonly IGroupRepository _groupRepository;

    public TaskAccessEvaluator(
        IFormAccessRuleRepository formAccessRuleRepository,
        IGroupRepository groupRepository)
    {
        _formAccessRuleRepository = formAccessRuleRepository;
        _groupRepository = groupRepository;
    }

    public async Task<bool> CanUserAccessTaskAsync(
        Guid userId,
        HumanTask task,
        Guid formDefinitionId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        if (task.AssignedUserId.HasValue && task.AssignedUserId.Value != userId)
        {
            return false;
        }

        if (task.AssignedUserId is null && task.ClaimedByUserId.HasValue && task.ClaimedByUserId.Value != userId)
        {
            return false;
        }

        var rules = await _formAccessRuleRepository.ListByFormDefinitionIdAsync(formDefinitionId, cancellationToken);
        if (rules.Count == 0)
        {
            return false;
        }

        var userGroupsMap = await _groupRepository.ListGroupIdsByUserIdsAsync([userId], cancellationToken);
        var userGroupIds = userGroupsMap.TryGetValue(userId, out var groups)
            ? new HashSet<Guid>(groups)
            : new HashSet<Guid>();

        foreach (var rule in rules)
        {
            var matched = rule.AccessScope switch
            {
                FormAccessScope.DirectUser => rule.UserId == userId,
                FormAccessScope.WholeGroup => rule.GroupId.HasValue && userGroupIds.Contains(rule.GroupId.Value),
                FormAccessScope.GroupUserSubset => rule.GroupId.HasValue
                    && userGroupIds.Contains(rule.GroupId.Value)
                    && rule.UserId == userId,
                _ => false
            };

            if (matched)
            {
                return true;
            }
        }

        return false;
    }
}
