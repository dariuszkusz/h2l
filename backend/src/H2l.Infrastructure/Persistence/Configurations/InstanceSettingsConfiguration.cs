using H2l.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace H2l.Infrastructure.Persistence.Configurations;

public sealed class InstanceSettingsConfiguration : IEntityTypeConfiguration<InstanceSettings>
{
    public void Configure(EntityTypeBuilder<InstanceSettings> builder)
    {
        builder.ToTable("instance_settings", "app");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.InstanceId)
            .IsRequired();

        builder.Property(x => x.SelectedPalette)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.DefaultLanguage)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.EncryptedLicenseKey)
            .HasColumnType("text");

        builder.Property(x => x.EncryptedSmtpConfig)
            .HasColumnType("text");

        builder.Property(x => x.DefaultRetentionDays)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.TenantId)
            .IsUnique();

        builder.HasIndex(x => x.InstanceId)
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithOne()
            .HasForeignKey<InstanceSettings>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
