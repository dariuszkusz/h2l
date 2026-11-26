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
public sealed class Stage2SchemaAndRepositoriesTests
{
    private const string MinimalSchema = """
    {
      "schemaVersion": "1.0",
      "defaultLanguage": "en",
      "layoutMode": "single",
      "steps": [
        {
          "id": "step_1",
          "title": { "en": "Step" },
          "components": [
            { "id": "cmp_1", "type": "text", "variableKey": "note" }
          ]
        }
      ]
    }
    """;

    private readonly PostgreSqlContainerFixture _fixture;

    public Stage2SchemaAndRepositoriesTests(PostgreSqlContainerFixture fixture)
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
    public async Task Stage2Migration_CreatesAllStage2Tables()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var expectedTables = new[]
        {
            "form_versions",
            "human_tasks",
            "form_submissions",
            "submission_attachments",
            "outbox_messages"
        };

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'app';
            """;

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await cmd.ExecuteReaderAsync())
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
    }

    [Fact]
    public async Task HumanTaskRepository_FiltersByAclWholeGroupSubsetAndDirectUser_AndDetectsClaimConcurrency()
    {
        var tenant = Tenant.Create("Stage2 Tenant", $"s2-{Guid.CreateVersion7():N}").Value;
        var userA = User.Create(tenant.Id, $"a-{Guid.CreateVersion7():N}@example.com", "hash", SystemRole.EndUser).Value;
        var userB = User.Create(tenant.Id, $"b-{Guid.CreateVersion7():N}@example.com", "hash", SystemRole.EndUser).Value;
        var groupSales = Group.Create(tenant.Id, "Sales", $"sales-{Guid.CreateVersion7():N}").Value;
        var membershipA = UserGroup.Create(tenant.Id, userA.Id, groupSales.Id).Value;
        var membershipB = UserGroup.Create(tenant.Id, userB.Id, groupSales.Id).Value;

        var project = Project.Create(tenant.Id, "Procurement").Value;
        var process = WorkflowProcess.Create(tenant.Id, project.Id, "Software Buy", $"buy-{Guid.CreateVersion7():N}", status: WorkflowProcessStatus.Active).Value;
        var formSubset = FormDefinition.Create(tenant.Id, process.Id, "Subset Form", "step-subset", MinimalSchema, FormDefinitionStatus.Published).Value;
        var versionSubset = FormVersion.Create(tenant.Id, formSubset.Id, 1, MinimalSchema, userA.Id).Value;
        var ruleSubset = FormAccessRule.Create(tenant.Id, formSubset.Id, groupSales.Id, userA.Id).Value;
        var taskSubset = HumanTask.Create(tenant.Id, versionSubset.Id, $"ext-subset-{Guid.CreateVersion7():N}").Value;

        await using (var seedDb = CreateDbContext())
        {
            seedDb.Tenants.Add(tenant);
            seedDb.Users.AddRange(userA, userB);
            seedDb.Groups.Add(groupSales);
            seedDb.UserGroups.AddRange(membershipA, membershipB);
            seedDb.Projects.Add(project);
            seedDb.WorkflowProcesses.Add(process);
            seedDb.FormDefinitions.Add(formSubset);
            seedDb.FormVersions.Add(versionSubset);
            seedDb.FormAccessRules.Add(ruleSubset);
            seedDb.HumanTasks.Add(taskSubset);
            await seedDb.SaveChangesAsync();
        }

        // User A is in GroupUserSubset -> sees task; User B is in same group but NOT subset -> does NOT see task
        await using (var db = CreateDbContext(tenant.Id))
        {
            var repo = new HumanTaskRepository(db);
            var listA = await repo.ListForUserAsync(userA.Id, [groupSales.Id], null, TaskListScope.All, 1, 20);
            var listB = await repo.ListForUserAsync(userB.Id, [groupSales.Id], null, TaskListScope.All, 1, 20);

            Assert.Single(listA.Items);
            Assert.Equal(taskSubset.Id, listA.Items[0].Id);
            Assert.Empty(listB.Items);
        }

        // Optimistic concurrency conflict on simultaneous Claim
        await using var db1 = CreateDbContext(tenant.Id);
        await using var db2 = CreateDbContext(tenant.Id);
        var repo1 = new HumanTaskRepository(db1);
        var repo2 = new HumanTaskRepository(db2);
        var uow1 = new UnitOfWork(db1);
        var uow2 = new UnitOfWork(db2);

        var loaded1 = await repo1.GetByIdForUpdateAsync(taskSubset.Id);
        var loaded2 = await repo2.GetByIdForUpdateAsync(taskSubset.Id);

        Assert.True(loaded1!.Claim(userA.Id).IsSuccess);
        Assert.True(loaded2!.Claim(userB.Id).IsSuccess);

        Assert.True(await uow1.TrySaveChangesAsync());
        Assert.False(await uow2.TrySaveChangesAsync());
    }

    [Fact]
    public async Task FormSubmissionsAndOutbox_EnforceIdempotencyUniquenessAndTenantIsolation()
    {
        var tenantA = Tenant.Create("Iso A", $"ia-{Guid.CreateVersion7():N}").Value;
        var tenantB = Tenant.Create("Iso B", $"ib-{Guid.CreateVersion7():N}").Value;
        var userA = User.Create(tenantA.Id, $"ua-{Guid.CreateVersion7():N}@example.com", "hash", SystemRole.EndUser).Value;
        var projectA = Project.Create(tenantA.Id, "Proj A").Value;
        var processA = WorkflowProcess.Create(tenantA.Id, projectA.Id, "Proc A", $"pa-{Guid.CreateVersion7():N}", status: WorkflowProcessStatus.Active).Value;
        var formA = FormDefinition.Create(tenantA.Id, processA.Id, "Form A", "step-a", MinimalSchema, FormDefinitionStatus.Published).Value;
        var versionA = FormVersion.Create(tenantA.Id, formA.Id, 1, MinimalSchema, userA.Id).Value;
        var taskA = HumanTask.Create(tenantA.Id, versionA.Id, $"ext-a-{Guid.CreateVersion7():N}", assignedUserId: userA.Id).Value;
        var submissionA = FormSubmission.Create(
            Guid.CreateVersion7(), tenantA.Id, taskA.Id, versionA.Id, userA.Id,
            "idem-unique-1", "{\"note\":\"ok\"}", null, DateTimeOffset.UtcNow.AddDays(90), DateTimeOffset.UtcNow).Value;
        var outboxA = OutboxMessage.Create(tenantA.Id, taskA.Id, submissionA.Id, "TASK_SUBMITTED").Value;

        await using (var seedDb = CreateDbContext())
        {
            seedDb.Tenants.AddRange(tenantA, tenantB);
            seedDb.Users.Add(userA);
            seedDb.Projects.Add(projectA);
            seedDb.WorkflowProcesses.Add(processA);
            seedDb.FormDefinitions.Add(formA);
            seedDb.FormVersions.Add(versionA);
            seedDb.HumanTasks.Add(taskA);
            seedDb.FormSubmissions.Add(submissionA);
            seedDb.OutboxMessages.Add(outboxA);
            await seedDb.SaveChangesAsync();
        }

        // Duplicate (tenant_id, human_task_id, idempotency_key) rejected at DB level
        await using (var dupDb = CreateDbContext(tenantA.Id))
        {
            var dup = FormSubmission.Create(
                Guid.CreateVersion7(), tenantA.Id, taskA.Id, versionA.Id, userA.Id,
                "idem-unique-1", "{}", null, null, DateTimeOffset.UtcNow).Value;
            dupDb.FormSubmissions.Add(dup);
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dupDb.SaveChangesAsync());
            var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
            Assert.Equal("23505", pgEx.SqlState);
        }

        // Tenant B cannot see Tenant A's Stage 2 records
        await using (var dbB = CreateDbContext(tenantB.Id))
        {
            Assert.Empty(await dbB.FormVersions.AsNoTracking().ToListAsync());
            Assert.Empty(await dbB.HumanTasks.AsNoTracking().ToListAsync());
            Assert.Empty(await dbB.FormSubmissions.AsNoTracking().ToListAsync());
            Assert.Empty(await dbB.OutboxMessages.AsNoTracking().ToListAsync());
        }
    }
}
