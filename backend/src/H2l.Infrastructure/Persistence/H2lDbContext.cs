using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence;

public sealed class H2lDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public Guid? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<InstanceSettings> InstanceSettings => Set<InstanceSettings>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserActivationToken> UserActivationTokens => Set<UserActivationToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkflowProcess> WorkflowProcesses => Set<WorkflowProcess>();

    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();

    public DbSet<FormVersion> FormVersions => Set<FormVersion>();

    public DbSet<FormAccessRule> FormAccessRules => Set<FormAccessRule>();

    public DbSet<HumanTask> HumanTasks => Set<HumanTask>();

    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public H2lDbContext(
        DbContextOptions<H2lDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(H2lDbContext).Assembly);

        modelBuilder.Entity<InstanceSettings>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(e => (CurrentTenantId == null || e.TenantId == CurrentTenantId) && e.DeletedAt == null);

        modelBuilder.Entity<UserActivationToken>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<Group>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<UserGroup>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<Project>()
            .HasQueryFilter(e => (CurrentTenantId == null || e.TenantId == CurrentTenantId) && e.DeletedAt == null);

        modelBuilder.Entity<WorkflowProcess>()
            .HasQueryFilter(e => (CurrentTenantId == null || e.TenantId == CurrentTenantId) && e.DeletedAt == null);

        modelBuilder.Entity<FormDefinition>()
            .HasQueryFilter(e => (CurrentTenantId == null || e.TenantId == CurrentTenantId) && e.DeletedAt == null);

        modelBuilder.Entity<FormAccessRule>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<FormVersion>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<HumanTask>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<FormSubmission>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<SubmissionAttachment>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<OutboxMessage>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
    }
}
