using H2l.Domain.Identity;
using H2l.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class SubmissionAttachmentConfiguration : IEntityTypeConfiguration<SubmissionAttachment>
{
    public void Configure(EntityTypeBuilder<SubmissionAttachment> builder)
    {
        builder.ToTable("submission_attachments", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.HumanTaskId)
            .IsRequired();

        builder.Property(x => x.FormSubmissionId);

        builder.Property(x => x.FieldVariableKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.StoragePath)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.IsEncrypted)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.HumanTaskId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<HumanTask>()
            .WithMany()
            .HasForeignKey(x => x.HumanTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FormSubmission>()
            .WithMany()
            .HasForeignKey(x => x.FormSubmissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
