using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class WorkflowConnectorConfiguration : IEntityTypeConfiguration<WorkflowConnector>
{
    public void Configure(EntityTypeBuilder<WorkflowConnector> builder)
    {
        builder.ToTable("workflow_connectors", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ConnectorType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.AuthType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.CallbackUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.EncryptedConfig)
            .HasColumnType("text");

        builder.Property(x => x.HasCredentials)
            .IsRequired();

        builder.Property(x => x.HasCustomCaCertificate)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(x => new { x.TenantId, x.ConnectorType, x.IsActive });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
