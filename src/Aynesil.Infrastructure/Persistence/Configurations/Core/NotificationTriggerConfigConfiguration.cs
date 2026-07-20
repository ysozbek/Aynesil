using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class NotificationTriggerConfigConfiguration
    : IEntityTypeConfiguration<NotificationTriggerConfig>
{
    public void Configure(EntityTypeBuilder<NotificationTriggerConfig> builder)
    {
        builder.ToTable("notification_trigger_config", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id");
        builder.Property(x => x.TriggerCode).HasColumnName("trigger_code").IsRequired();
        builder.Property(x => x.TemplateId).HasColumnName("template_id");
        builder.Property(x => x.OffsetMinutes).HasColumnName("offset_minutes").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.CorporationId, x.TriggerCode })
            .HasDatabaseName("ix_notification_trigger_config_corp");

        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Channels)
            .WithOne(ch => ch.TriggerConfig)
            .HasForeignKey(ch => ch.TriggerConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
