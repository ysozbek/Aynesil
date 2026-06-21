using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.recurring_schedule.
/// Audit: created_at, created_by, updated_at, row_version — no updated_by / deleted_at in DDL.
/// by_weekday and by_monthday are PostgreSQL smallint[] arrays.
/// </summary>
public class RecurringScheduleConfiguration : IEntityTypeConfiguration<RecurringSchedule>
{
    public void Configure(EntityTypeBuilder<RecurringSchedule> builder)
    {
        builder.ToTable("recurring_schedule", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.StudentProgramId).HasColumnName("student_program_id");
        builder.Property(x => x.SessionTypeId).HasColumnName("session_type_id");
        builder.Property(x => x.RoomId).HasColumnName("room_id");
        builder.Property(x => x.Frequency).HasColumnName("frequency").IsRequired();
        builder.Property(x => x.IntervalCount).HasColumnName("interval_count").HasDefaultValue(1).IsRequired();

        builder.Property(x => x.ByWeekday)
            .HasColumnName("by_weekday")
            .HasColumnType("smallint[]");

        builder.Property(x => x.ByMonthday)
            .HasColumnName("by_monthday")
            .HasColumnType("smallint[]");

        builder.Property(x => x.StartTime).HasColumnName("start_time").IsRequired();
        builder.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
        builder.Property(x => x.RangeStart).HasColumnName("range_start").IsRequired();
        builder.Property(x => x.RangeEnd).HasColumnName("range_end");
        builder.Property(x => x.MaxOccurrences).HasColumnName("max_occurrences");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // Not in DDL
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);

        builder.HasMany(x => x.Exceptions)
            .WithOne()
            .HasForeignKey(e => e.RecurringScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Sessions)
            .WithOne()
            .HasForeignKey(s => s.RecurringScheduleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
