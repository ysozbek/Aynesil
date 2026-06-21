using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.recurrence_exception.
/// Unique: (recurring_schedule_id, exception_date).
/// No audit fields in DDL. Cascade-deleted with the parent recurring schedule.
/// </summary>
public class RecurrenceExceptionConfiguration : IEntityTypeConfiguration<RecurrenceException>
{
    public void Configure(EntityTypeBuilder<RecurrenceException> builder)
    {
        builder.ToTable("recurrence_exception", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.RecurringScheduleId).HasColumnName("recurring_schedule_id").IsRequired();
        builder.Property(x => x.ExceptionDate).HasColumnName("exception_date").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").IsRequired();
        builder.Property(x => x.NewStartAt).HasColumnName("new_start_at");
        builder.Property(x => x.Reason).HasColumnName("reason");

        builder.HasIndex(x => new { x.RecurringScheduleId, x.ExceptionDate })
            .IsUnique()
            .HasDatabaseName("recurrence_exception_recurring_schedule_id_exception_date_key");
    }
}
