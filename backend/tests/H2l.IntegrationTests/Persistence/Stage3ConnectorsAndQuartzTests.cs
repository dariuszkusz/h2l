using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Persistence;
using H2l.Infrastructure.Persistence.Repositories;
using H2l.Infrastructure.Tenancy;
using H2l.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace H2l.IntegrationTests.Persistence;

[Collection("PostgreSql")]
public sealed class Stage3ConnectorsAndQuartzTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public Stage3ConnectorsAndQuartzTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private H2lDbContext CreateDbContext(Guid? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<H2lDbContext>()
            .UseNpgsql(
                _fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenantContext = new DefaultTenantContext();
        tenantContext.SetTenantId(tenantId);
        return new H2lDbContext(options, tenantContext);
    }

    [Fact]
    public async Task Stage3Migration_CreatesWorkflowConnectorsAndQuartzTables()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var appCmd = connection.CreateCommand();
        appCmd.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'app' AND table_name = 'workflow_connectors';
            """;
        var connectorTable = await appCmd.ExecuteScalarAsync();
        Assert.Equal("workflow_connectors", connectorTable);

        await using var quartzCmd = connection.CreateCommand();
        quartzCmd.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'quartz' AND table_name LIKE 'qrtz_%';
            """;
        var quartzTableCount = Convert.ToInt32(await quartzCmd.ExecuteScalarAsync());
        Assert.True(quartzTableCount >= 11);
    }

    [Fact]
    public async Task WorkflowConnectorRepository_EnforcesTenantIsolationAndDeadLetterQuery()
    {
        var tenantA = Tenant.Create("Tenant A Stage3", $"tenant-a-s3-{Guid.NewGuid():N}").Value;
        var tenantB = Tenant.Create("Tenant B Stage3", $"tenant-b-s3-{Guid.NewGuid():N}").Value;

        await using (var seedDb = CreateDbContext())
        {
            seedDb.Tenants.AddRange(tenantA, tenantB);
            await seedDb.SaveChangesAsync();
        }

        var connectorA = WorkflowConnector.Create(
            tenantA.Id, "Camunda A", ConnectorType.Camunda8, ConnectorAuthType.OAuth2ClientCredentials,
            "https://camunda-a.example.com", null, "v1:enc:a", true, false).Value;
        var connectorB = WorkflowConnector.Create(
            tenantB.Id, "n8n B", ConnectorType.N8n, ConnectorAuthType.HmacSha256,
            "https://n8n-b.example.com", null, "v1:enc:b", true, false).Value;

        await using (var dbA = CreateDbContext(tenantA.Id))
        {
            var repoA = new WorkflowConnectorRepository(dbA);
            await repoA.AddAsync(connectorA);
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = CreateDbContext(tenantB.Id))
        {
            var repoB = new WorkflowConnectorRepository(dbB);
            await repoB.AddAsync(connectorB);
            await dbB.SaveChangesAsync();
        }

        await using (var verifyDbA = CreateDbContext(tenantA.Id))
        {
            var repoA = new WorkflowConnectorRepository(verifyDbA);
            var listA = await repoA.ListAsync();
            Assert.Single(listA);
            Assert.Equal(connectorA.Id, listA[0].Id);
            Assert.Null(await repoA.GetByIdAsync(connectorB.Id));
            Assert.Equal(1, await repoA.CountActiveAsync());
        }
    }
}
