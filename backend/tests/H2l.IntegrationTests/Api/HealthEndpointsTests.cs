using System.Net;
using H2l.IntegrationTests.Infrastructure;
using Xunit;

namespace H2l.IntegrationTests.Api;

[Collection("PostgreSql")]
public sealed class HealthEndpointsTests
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(PostgreSqlContainerFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/api/v1/health/live")]
    public async Task LiveEndpoint_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/ready")]
    [InlineData("/api/v1/health/ready")]
    public async Task ReadyEndpoint_WithHealthyDependencies_ReturnsOk(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
