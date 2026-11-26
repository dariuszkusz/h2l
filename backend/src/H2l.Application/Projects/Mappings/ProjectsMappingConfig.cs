using H2l.Application.Projects.DTOs;
using H2l.Domain.Workflows;
using Mapster;

namespace H2l.Application.Projects.Mappings;

public sealed class ProjectsMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectResponse>();
        config.NewConfig<WorkflowProcess, WorkflowProcessResponse>();
    }
}
