using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
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
public sealed class Stage1SchemaAndRepositoriesTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public Stage1SchemaAndRepositoriesTests(PostgreSqlContainerFixture fixture)
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
    public async Task Stage1Migration_CreatesAllExpectedTablesAndNoConnectorFk()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var expectedTables = new[]
        {
            "groups",
            "user_groups",
            "user_activation_tokens",
            "refresh_tokens",
            "projects",
            "workflow_processes",
            "form_definitions",
            "form_access_rules"
        };

        await using var tableCmd = connection.CreateCommand();
        tableCmd.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'app';
            """;

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await tableCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var table in expectedTables)
        {
            Assert.Contains(table, tables);
        }

        await using var fkCmd = connection.CreateCommand();
        fkCmd.CommandText = """
            SELECT kcu.column_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
              ON tc.constraint_name = kcu.constraint_name
             AND tc.table_schema = kcu.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND tc.table_schema = 'app'
              AND tc.table_name = 'workflow_processes'
              AND kcu.column_name = 'connector_id';
            """;

        var connectorFk = await fkCmd.ExecuteScalarAsync();
        Assert.Null(connectorFk);
    }

    [Fact]
    public async Task FormAccessRules_CheckConstraint_RejectsBothGroupAndUserNull()
    {
        var tenant = Tenant.Create("Check Tenant", $"check-{Guid.CreateVersion7():N}").Value;
        var project = Project.Create(tenant.Id, "Check Project").Value;
        var process = WorkflowProcess.Create(tenant.Id, project.Id, "Check Process", "check-proc").Value;
        var form = FormDefinition.Create(tenant.Id, process.Id, "Check Form", "step-1").Value;

        await using (var db = CreateDbContext())
        {
            db.Tenants.Add(tenant);
            db.Projects.Add(project);
            db.WorkflowProcesses.Add(process);
            db.FormDefinitions.Add(form);
            await db.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO app.form_access_rules (id, tenant_id, form_definition_id, group_id, user_id, access_scope)
            VALUES (@id, @tenant_id, @form_definition_id, NULL, NULL, 'WholeGroup');
            """;
        cmd.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmd.Parameters.AddWithValue("tenant_id", tenant.Id);
        cmd.Parameters.AddWithValue("form_definition_id", form.Id);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_form_access_rules_target_required", ex.ConstraintName);
    }

    [Fact]
    public async Task PartialUniqueIndexes_AllowReuseAfterSoftDeleteAndBlockActiveDuplicates()
    {
        var tenant = Tenant.Create("Unique Tenant", $"uniq-{Guid.CreateVersion7():N}").Value;
        var project = Project.Create(tenant.Id, "Unique Project").Value;
        var deletedProcess = WorkflowProcess.Create(tenant.Id, project.Id, "Order Flow", "order-flow").Value;
        deletedProcess.SoftDelete();

        await using (var db = CreateDbContext())
        {
            db.Tenants.Add(tenant);
            db.Projects.Add(project);
            db.WorkflowProcesses.Add(deletedProcess);
            await db.SaveChangesAsync();
        }

        var activeProcess1 = WorkflowProcess.Create(tenant.Id, project.Id, "Order Flow V2", "order-flow").Value;
        await using (var db = CreateDbContext(tenant.Id))
        {
            db.WorkflowProcesses.Add(activeProcess1);
            await db.SaveChangesAsync();
        }

        var duplicateActive = WorkflowProcess.Create(tenant.Id, project.Id, "Order Flow Dup", "order-flow").Value;
        await using (var db = CreateDbContext(tenant.Id))
        {
            db.WorkflowProcesses.Add(duplicateActive);
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
            Assert.Equal("23505", pgEx.SqlState);
        }
    }

    [Fact]
    public async Task GlobalQueryFilters_IsolateTenantsAndSoftDeletesAcrossStage1Entities()
    {
        var tenantA = Tenant.Create("Tenant A", $"ta-{Guid.CreateVersion7():N}").Value;
        var tenantB = Tenant.Create("Tenant B", $"tb-{Guid.CreateVersion7():N}").Value;

        var projectA = Project.Create(tenantA.Id, "Project A").Value;
        var deletedProjectA = Project.Create(tenantA.Id, "Deleted Project A").Value;
        deletedProjectA.SoftDelete();
        var projectB = Project.Create(tenantB.Id, "Project B").Value;

        var groupA = Group.Create(tenantA.Id, "Group A", "grp-a").Value;
        var groupB = Group.Create(tenantB.Id, "Group B", "grp-b").Value;

        await using (var seedDb = CreateDbContext())
        {
            seedDb.Tenants.AddRange(tenantA, tenantB);
            seedDb.Projects.AddRange(projectA, deletedProjectA, projectB);
            seedDb.Groups.AddRange(groupA, groupB);
            await seedDb.SaveChangesAsync();
        }

        await using var scopedDbA = CreateDbContext(tenantA.Id);
        var visibleProjects = await scopedDbA.Projects.AsNoTracking().ToListAsync();
        var visibleGroups = await scopedDbA.Groups.AsNoTracking().ToListAsync();

        Assert.Single(visibleProjects);
        Assert.Equal(projectA.Id, visibleProjects[0].Id);
        Assert.Single(visibleGroups);
        Assert.Equal(groupA.Id, visibleGroups[0].Id);
    }

    [Fact]
    public async Task AuditLogRepository_AppendsAndQueriesScopedByTenant()
    {
        var tenantA = Tenant.Create("Audit Tenant A", $"aa-{Guid.CreateVersion7():N}").Value;
        var tenantB = Tenant.Create("Audit Tenant B", $"ab-{Guid.CreateVersion7():N}").Value;
        var resourceId = Guid.CreateVersion7();

        await using (var seedDb = CreateDbContext())
        {
            seedDb.Tenants.AddRange(tenantA, tenantB);
            await seedDb.SaveChangesAsync();
        }

        await using (var dbA = CreateDbContext(tenantA.Id))
        {
            var repo = new AuditLogRepository(dbA);
            var uow = new UnitOfWork(dbA);
            var entry = AuditLog.Create(tenantA.Id, null, "USER_INVITED", "User", resourceId, "{\"role\":\"EndUser\"}").Value;
            await repo.AddAsync(entry);
            await uow.SaveChangesAsync();
        }

        await using (var dbA = CreateDbContext(tenantA.Id))
        {
            var repo = new AuditLogRepository(dbA);
            var entries = await repo.ListByResourceAsync("User", resourceId);
            Assert.Single(entries);
            Assert.Equal("USER_INVITED", entries[0].Action);
        }

        await using (var dbB = CreateDbContext(tenantB.Id))
        {
            var repo = new AuditLogRepository(dbB);
            var entries = await repo.ListByResourceAsync("User", resourceId);
            Assert.Empty(entries);
        }
    }
}
