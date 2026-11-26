using H2l.Domain.Identity;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class FormAccessRuleConfiguration : IEntityTypeConfiguration<FormAccessRule>
{
    public void Configure(EntityTypeBuilder<FormAccessRule> builder)
    {
        builder.ToTable(
            "form_access_rules",
            "app",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_form_access_rules_target_required",
                "group_id IS NOT NULL OR user_id IS NOT NULL"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.FormDefinitionId)
            .IsRequired();

        builder.Property(x => x.GroupId);

        builder.Property(x => x.UserId);

        builder.Property(x => x.AccessScope)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.FormDefinitionId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FormDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FormDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
