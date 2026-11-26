using H2l.Application.Projects.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Workflows;
using MapsterMapper;

namespace H2l.Application.Projects.Queries;

public sealed class ListProjectProcessesQueryHandler
{
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkflowProcessRepository _workflowProcessRepository;
    private readonly IMapper _mapper;

    public ListProjectProcessesQueryHandler(
        IProjectRepository projectRepository,
        IWorkflowProcessRepository workflowProcessRepository,
        IMapper mapper)
    {
        _projectRepository = projectRepository;
        _workflowProcessRepository = workflowProcessRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<WorkflowProcessResponse>>> Handle(
        ListProjectProcessesQuery query,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<IReadOnlyList<WorkflowProcessResponse>>(
                Error.NotFound("PROJECT_NOT_FOUND", "Project was not found."));
        }

        var processes = await _workflowProcessRepository.ListByProjectIdAsync(query.ProjectId, cancellationToken);
        var items = processes.Select(p => _mapper.Map<WorkflowProcessResponse>(p)).ToList();
        return Result.Success<IReadOnlyList<WorkflowProcessResponse>>(items);
    }
}
