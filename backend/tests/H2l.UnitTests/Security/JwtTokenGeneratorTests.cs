using System.Security.Claims;
using H2l.Application.Common;
using H2l.Domain.Identity;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Security;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaimsAndLifetime()
    {
        var now = new DateTimeOffset(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);

        var options = Options.Create(new JwtOptions
        {
            Issuer = "h2l-test",
            Audience = "h2l-pwa",
            SigningKey = "super-secret-unit-test-signing-key-32-chars",
            AccessTokenLifetimeMinutes = 20,
            RefreshTokenLifetimeDays = 14
        });

        var generator = new JwtTokenGenerator(options, dateTimeProvider);
        var tenantId = Guid.CreateVersion7();
        var user = User.Create(tenantId, "admin@example.com", "hashed", SystemRole.Admin).Value;

        var token = generator.GenerateAccessToken(user);

        Assert.Equal(1200, token.ExpiresInSeconds);
        Assert.Equal(14, token.RefreshTokenLifetimeDays);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal("admin@example.com", jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(tenantId.ToString(), jwt.GetClaim("tenant_id").Value);
        Assert.Equal("Admin", jwt.GetClaim(ClaimTypes.Role).Value);
    }
}
