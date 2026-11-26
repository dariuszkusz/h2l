using H2l.Application.Projects.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Workflows;
using MapsterMapper;

namespace H2l.Application.Projects.Queries;

public sealed class ListProjectsQueryHandler
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;

    public ListProjectsQueryHandler(IProjectRepository projectRepository, IMapper mapper)
    {
        _projectRepository = projectRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ProjectResponse>>> Handle(
        ListProjectsQuery query,
        CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.ListAsync(cancellationToken);
        var items = projects.Select(p => _mapper.Map<ProjectResponse>(p)).ToList();
        return Result.Success<IReadOnlyList<ProjectResponse>>(items);
    }
}
