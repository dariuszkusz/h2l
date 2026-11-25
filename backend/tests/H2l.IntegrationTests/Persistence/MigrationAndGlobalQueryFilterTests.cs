using H2l.Domain.Identity;
using H2l.Infrastructure.Persistence;
using H2l.Infrastructure.Tenancy;
using H2l.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace H2l.IntegrationTests.Persistence;

[Collection("PostgreSql")]
public sealed class MigrationAndGlobalQueryFilterTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public MigrationAndGlobalQueryFilterTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InitialMigration_CreatesAppAuditAndQuartzSchemas()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT schema_name
            FROM information_schema.schemata
            WHERE schema_name IN ('app', 'audit', 'quartz');
            """;

        var schemas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        Assert.Contains("app", schemas);
        Assert.Contains("audit", schemas);
        Assert.Contains("quartz", schemas);
    }

    [Fact]
    public async Task GlobalQueryFilter_IsolatesTenantsAndExcludesSoftDeletedUsers()
    {
        var options = new DbContextOptionsBuilder<H2lDbContext>()
            .UseNpgsql(
                _fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenantA = Tenant.Create("Tenant A", $"tenant-a-{Guid.CreateVersion7():N}").Value;
        var tenantB = Tenant.Create("Tenant B", $"tenant-b-{Guid.CreateVersion7():N}").Value;

        var activeUserA = User.Create(tenantA.Id, "active-a@example.com", "hash-a", SystemRole.EndUser).Value;
        var deletedUserA = User.Create(tenantA.Id, "deleted-a@example.com", "hash-del", SystemRole.EndUser).Value;
        deletedUserA.SoftDelete();
        var activeUserB = User.Create(tenantB.Id, "active-b@example.com", "hash-b", SystemRole.EndUser).Value;

        await using (var seedContext = new H2lDbContext(options, new DefaultTenantContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Users.AddRange(activeUserA, deletedUserA, activeUserB);
            await seedContext.SaveChangesAsync();
        }

        var tenantAContext = new DefaultTenantContext();
        tenantAContext.SetTenantId(tenantA.Id);

        await using (var scopedContext = new H2lDbContext(options, tenantAContext))
        {
            var visibleUsers = await scopedContext.Users.AsNoTracking().ToListAsync();

            Assert.Single(visibleUsers);
            Assert.Equal(activeUserA.Id, visibleUsers[0].Id);
        }
    }
}
