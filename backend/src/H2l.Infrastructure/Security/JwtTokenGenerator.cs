using System.Security.Claims;
using System.Text;
using H2l.Application.Common;
using H2l.Domain.Identity;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace H2l.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public JwtTokenGenerator(
        IOptions<JwtOptions> jwtOptions,
        IDateTimeProvider dateTimeProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _dateTimeProvider = dateTimeProvider;
    }

    public GeneratedAccessToken GenerateAccessToken(User user)
    {
        var now = _dateTimeProvider.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
            ["tenant_id"] = user.TenantId.ToString(),
            [ClaimTypes.Role] = user.SystemRole.ToString()
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            Claims = claims,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            SigningCredentials = credentials
        };

        var accessToken = _tokenHandler.CreateToken(descriptor);
        return new GeneratedAccessToken(
            accessToken,
            _jwtOptions.AccessTokenLifetimeMinutes * 60,
            _jwtOptions.RefreshTokenLifetimeDays);
    }
}
