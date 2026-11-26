using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class WorkflowProcessConfiguration : IEntityTypeConfiguration<WorkflowProcess>
{
    public void Configure(EntityTypeBuilder<WorkflowProcess> builder)
    {
        builder.ToTable("workflow_processes", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ProjectId)
            .IsRequired();

        // Deferred FK to app.workflow_connectors in Stage 2: nullable uuid column with no FK in Stage 1.
        builder.Property(x => x.ConnectorId);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ExternalProcessKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.ExternalProcessKey })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.ConnectorId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowConnector>()
            .WithMany()
            .HasForeignKey(x => x.ConnectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
