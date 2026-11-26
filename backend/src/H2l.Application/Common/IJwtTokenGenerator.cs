using H2l.Domain.Identity;

namespace H2l.Application.Common;

public interface IJwtTokenGenerator
{
    GeneratedAccessToken GenerateAccessToken(User user);
}
