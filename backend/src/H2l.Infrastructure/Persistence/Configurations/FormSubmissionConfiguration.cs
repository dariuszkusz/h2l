using H2l.Domain.Identity;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.ToTable("form_submissions", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.HumanTaskId)
            .IsRequired();

        builder.Property(x => x.FormVersionId)
            .IsRequired();

        builder.Property(x => x.SubmittedByUserId)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.PublicDataJsonb)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.EncryptedSensitiveData)
            .HasColumnType("text");

        builder.Property(x => x.RetentionUntil);

        builder.Property(x => x.AnonymizedAt);

        builder.Property(x => x.SubmittedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.HumanTaskId, x.IdempotencyKey })
            .IsUnique();

        builder.HasIndex(x => x.RetentionUntil)
            .HasFilter("anonymized_at IS NULL");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<HumanTask>()
            .WithMany()
            .HasForeignKey(x => x.HumanTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FormVersion>()
            .WithMany()
            .HasForeignKey(x => x.FormVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
