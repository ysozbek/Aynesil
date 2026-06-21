using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.attendance.
/// Unique: (session_id, student_id). No standard audit, no soft delete.
/// reason_id references ref.ref_value (ref_type 'attendance_reason').
/// </summary>
public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendance", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.ReasonId).HasColumnName("reason_id");
        builder.Property(x => x.MinutesAttended).HasColumnName("minutes_attended");
        builder.Property(x => x.RecordedBy).HasColumnName("recorded_by");
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.Note).HasColumnName("note");

        builder.HasIndex(x => new { x.SessionId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("attendance_session_id_student_id_key");

        builder.HasIndex(x => new { x.CorporationId, x.StudentId, x.RecordedAt })
            .HasDatabaseName("ix_attendance_student");
    }
}
