using Aynesil.Domain.Modules.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Core;

public class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        // Maps to core.notification (C# class renamed to AppNotification to avoid conflicts)
        builder.ToTable("notification", schema: "core");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.TemplateId).HasColumnName("template_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id");
        builder.Property(x => x.Subject).HasColumnName("subject");
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("pending").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ReadAt).HasColumnName("read_at");

        // core.notification DDL: id, corporation_id, ..., created_at, read_at — no other audit columns
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.DeletedAt);

        builder.HasIndex(x => new { x.RecipientUserId, x.Status })
            .HasDatabaseName("ix_notification_recipient");

        builder.HasMany(x => x.Deliveries)
            .WithOne(d => d.Notification)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
