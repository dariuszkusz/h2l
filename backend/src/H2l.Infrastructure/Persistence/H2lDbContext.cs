using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2l.Infrastructure.Persistence;

public sealed class H2lDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public Guid? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<InstanceSettings> InstanceSettings => Set<InstanceSettings>();

    public DbSet<User> Users => Set<User>();

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

        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);
    }
}
