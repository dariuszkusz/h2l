using H2l.Application.Setup.Queries;
using H2l.Domain.Identity;
using Mapster;

namespace H2l.Application.Setup.Mappings;

public sealed class SetupMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InstanceSettings, SetupStatusResponse>()
            .MapWith(src => new SetupStatusResponse(false, src.InstanceId));
    }
}
