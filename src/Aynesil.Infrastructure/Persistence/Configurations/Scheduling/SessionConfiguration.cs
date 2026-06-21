using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.session.
/// Full audit: created_at, created_by, updated_at, updated_by, deleted_at, row_version.
/// The generated column time_range (tstzrange) and the composite FK helper column
/// session_type_ref_type are DB-managed and not mapped to the entity.
/// Room double-booking is enforced by EXCLUDE USING GIST on the table — the application
/// calls HasRoomConflictAsync as an early pre-check for a friendly error message.
/// </summary>
public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("session", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.SessionTypeId).HasColumnName("session_type_id").IsRequired();
        builder.Property(x => x.RoomId).HasColumnName("room_id");
        builder.Property(x => x.RecurringScheduleId).HasColumnName("recurring_schedule_id");
        builder.Property(x => x.ProgramServiceId).HasColumnName("program_service_id");
        builder.Property(x => x.Title).HasColumnName("title");
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("scheduled").IsRequired();
        builder.Property(x => x.IsMakeup).HasColumnName("is_makeup").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CancelReason).HasColumnName("cancel_reason");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CampusId, x.StartsAt })
            .HasDatabaseName("ix_session_campus_time");

        builder.HasIndex(x => new { x.RoomId, x.StartsAt })
            .HasDatabaseName("ix_session_room_time");

        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey(p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Educators)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Goals)
            .WithOne()
            .HasForeignKey(g => g.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(n => n.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Attendances)
            .WithOne()
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
