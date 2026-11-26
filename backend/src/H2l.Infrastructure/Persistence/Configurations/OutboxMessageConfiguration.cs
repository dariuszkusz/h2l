using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.HumanTaskId)
            .IsRequired();

        builder.Property(x => x.FormSubmissionId);

        builder.Property(x => x.MessageType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.NextAttemptAt)
            .IsRequired();

        builder.Property(x => x.DeadLetterReason)
            .HasMaxLength(2000);

        builder.Property(x => x.ProcessedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });

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

        builder.Ignore(x => x.DomainEvents);
    }
}
