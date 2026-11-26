using H2l.Domain.Identity;
using H2l.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class FormVersionConfiguration : IEntityTypeConfiguration<FormVersion>
{
    public void Configure(EntityTypeBuilder<FormVersion> builder)
    {
        builder.ToTable("form_versions", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.FormDefinitionId)
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .IsRequired();

        builder.Property(x => x.SchemaJsonb)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.PublishedByUserId)
            .IsRequired();

        builder.Property(x => x.PublishedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.FormDefinitionId, x.VersionNumber })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FormDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FormDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PublishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
