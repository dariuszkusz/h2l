using H2l.Infrastructure.Persistence;
using H2l.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace H2l.IntegrationTests.Infrastructure;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("h2l_test")
        .WithUsername("h2l_test")
        .WithPassword("h2l_test_password")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DB_CONNECTION_STRING"] = ConnectionString,
                        ["DB_AUTO_MIGRATE"] = "true",
                        ["JWT_ISSUER"] = "h2l-test",
                        ["JWT_AUDIENCE"] = "h2l-test-clients",
                        ["JWT_SIGNING_KEY"] = "integration-test-signing-key-with-at-least-32-chars"
                    });
                });
            });

        // Force host startup and explicitly ensure migrations + seed are applied
        // before any test class in the collection executes.
        using var warmupClient = Factory.CreateClient();
        await Factory.Services.ApplyDatabaseMigrationsAndSeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }
}
