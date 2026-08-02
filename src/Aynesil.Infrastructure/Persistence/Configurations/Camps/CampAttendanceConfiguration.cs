using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampAttendanceConfiguration : IEntityTypeConfiguration<CampAttendance>
{
    public void Configure(EntityTypeBuilder<CampAttendance> builder)
    {
        builder.ToTable("camp_attendance", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampEnrollmentId).HasColumnName("camp_enrollment_id").IsRequired();
        builder.Property(x => x.AttendanceDate).HasColumnName("attendance_date").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReasonId).HasColumnName("reason_id");
        builder.Property(x => x.RecordedBy).HasColumnName("recorded_by");

        // Unique attendance per enrollment per day — mirrors DB constraint.
        builder.HasIndex(x => new { x.CampEnrollmentId, x.AttendanceDate }).IsUnique();
    }
}
