using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class NotificationTriggerChannelConfiguration
    : IEntityTypeConfiguration<NotificationTriggerChannel>
{
    public void Configure(EntityTypeBuilder<NotificationTriggerChannel> builder)
    {
        builder.ToTable("notification_trigger_channel", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.TriggerConfigId).HasColumnName("trigger_config_id").IsRequired();
        builder.Property(x => x.ChannelId).HasColumnName("channel_id").IsRequired();

        builder.HasIndex(x => new { x.TriggerConfigId, x.ChannelId })
            .IsUnique()
            .HasDatabaseName("notification_trigger_channel_trigger_config_id_channel_id_key");

        builder.HasIndex(x => x.TriggerConfigId)
            .HasDatabaseName("ix_notification_trigger_channel_config");

        builder.HasOne(x => x.TriggerConfig)
            .WithMany(c => c.Channels)
            .HasForeignKey(x => x.TriggerConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
