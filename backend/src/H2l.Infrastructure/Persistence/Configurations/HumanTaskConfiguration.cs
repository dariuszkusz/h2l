using H2l.Domain.Identity;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class HumanTaskConfiguration : IEntityTypeConfiguration<HumanTask>
{
    public void Configure(EntityTypeBuilder<HumanTask> builder)
    {
        builder.ToTable("human_tasks", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ConnectorId);

        builder.Property(x => x.FormVersionId)
            .IsRequired();

        builder.Property(x => x.ExternalTaskId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.SensitiveDataMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.AssignedUserId);

        builder.Property(x => x.ClaimedByUserId);

        builder.Property(x => x.PrefilledDataJsonb)
            .HasColumnType("jsonb");

        builder.Property(x => x.EncryptedResumeUrl)
            .HasColumnType("text");

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ConcurrencyStamp)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ConnectorId, x.ExternalTaskId })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresAt });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FormVersion>()
            .WithMany()
            .HasForeignKey(x => x.FormVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ClaimedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
