using H2l.Application.Auth.DTOs;
using H2l.Domain.Identity;
using Mapster;

namespace H2l.Application.Auth.Mappings;

public sealed class AuthMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserProfileResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.ExternalId, src => src.ExternalId)
            .Map(dest => dest.SystemRole, src => src.SystemRole)
            .Map(dest => dest.PreferredLanguage, src => src.PreferredLanguage)
            .Map(dest => dest.PreferredTheme, src => src.PreferredTheme)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}
