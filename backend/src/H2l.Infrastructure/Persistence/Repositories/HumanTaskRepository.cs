using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence.Repositories;

public sealed class HumanTaskRepository : IHumanTaskRepository
{
    private readonly H2lDbContext _dbContext;

    public HumanTaskRepository(H2lDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<HumanTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.HumanTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<HumanTask?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.HumanTasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<HumanTaskDetailRecord?> GetDetailByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query =
            from task in _dbContext.HumanTasks.AsNoTracking()
            join version in _dbContext.FormVersions.AsNoTracking()
                on task.FormVersionId equals version.Id
            where task.Id == id
            select new HumanTaskDetailRecord(task, version, version.FormDefinitionId);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedHumanTasksResult> ListForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> userGroupIds,
        HumanTaskStatus? status,
        TaskListScope scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var groupIds = userGroupIds.ToArray();

        var allowedFormIdsQuery = _dbContext.FormAccessRules
            .AsNoTracking()
            .Where(rule =>
                (rule.AccessScope == FormAccessScope.DirectUser && rule.UserId == userId)
                || (rule.AccessScope == FormAccessScope.WholeGroup && rule.GroupId != null && groupIds.Contains(rule.GroupId.Value))
                || (rule.AccessScope == FormAccessScope.GroupUserSubset && rule.GroupId != null && groupIds.Contains(rule.GroupId.Value) && rule.UserId == userId))
            .Select(rule => rule.FormDefinitionId)
            .Distinct();

        var baseQuery =
            from task in _dbContext.HumanTasks.AsNoTracking()
            join version in _dbContext.FormVersions.AsNoTracking()
                on task.FormVersionId equals version.Id
            join form in _dbContext.FormDefinitions.AsNoTracking()
                on version.FormDefinitionId equals form.Id
            join process in _dbContext.WorkflowProcesses.AsNoTracking()
                on form.WorkflowProcessId equals process.Id
            join project in _dbContext.Projects.AsNoTracking()
                on process.ProjectId equals project.Id
            where allowedFormIdsQuery.Contains(form.Id)
            select new
            {
                Task = task,
                VersionNumber = version.VersionNumber,
                FormDefinitionId = form.Id,
                FormName = form.Name,
                ProcessName = process.Name,
                ProjectName = project.Name
            };

        if (status.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.Task.Status == status.Value);
        }

        baseQuery = scope switch
        {
            TaskListScope.Assigned => baseQuery.Where(x =>
                x.Task.AssignedUserId == userId || x.Task.ClaimedByUserId == userId),
            TaskListScope.GroupQueue => baseQuery.Where(x =>
                x.Task.AssignedUserId == null && x.Task.ClaimedByUserId == null),
            _ => baseQuery.Where(x =>
                x.Task.AssignedUserId == userId
                || (x.Task.AssignedUserId == null && (x.Task.ClaimedByUserId == null || x.Task.ClaimedByUserId == userId)))
        };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(x => x.Task.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HumanTaskListItem(
                x.Task.Id,
                x.FormDefinitionId,
                x.VersionNumber,
                x.FormName,
                x.ProcessName,
                x.ProjectName,
                x.Task.Status,
                x.Task.AssignedUserId,
                x.Task.ClaimedByUserId,
                x.Task.ExpiresAt,
                x.Task.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedHumanTasksResult(items, totalCount);
    }

    public async Task AddAsync(HumanTask task, CancellationToken cancellationToken = default)
        => await _dbContext.HumanTasks.AddAsync(task, cancellationToken);
}
