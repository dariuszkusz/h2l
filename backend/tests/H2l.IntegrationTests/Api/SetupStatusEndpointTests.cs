using System.Net;
using System.Net.Http.Json;
using H2l.Application.Setup.Queries;
using H2l.IntegrationTests.Infrastructure;
using Xunit;

namespace H2l.IntegrationTests.Api;

[Collection("PostgreSql")]
public sealed class SetupStatusEndpointTests
{
    private readonly HttpClient _client;

    public SetupStatusEndpointTests(PostgreSqlContainerFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetSetupStatus_OnFreshInstance_ReturnsNotInitializedWithUuidV7InstanceId()
    {
        var response = await _client.GetAsync("/api/v1/setup/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SetupStatusResponse>();
        Assert.NotNull(payload);
        Assert.False(payload.IsInitialized);
        Assert.NotEqual(Guid.Empty, payload.InstanceId);
        Assert.Equal(7, payload.InstanceId.Version);
    }
}
