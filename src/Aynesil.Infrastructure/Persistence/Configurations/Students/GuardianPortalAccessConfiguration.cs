using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.guardian_portal_access.
/// No audit columns. Unique: (guardian_id, student_id).
/// Revoke by setting revoked_at — never physically delete.
/// </summary>
public class GuardianPortalAccessConfiguration : IEntityTypeConfiguration<GuardianPortalAccess>
{
    public void Configure(EntityTypeBuilder<GuardianPortalAccess> builder)
    {
        builder.ToTable("guardian_portal_access", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.CanViewSessions).HasColumnName("can_view_sessions").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CanViewAttendance).HasColumnName("can_view_attendance").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CanViewReports).HasColumnName("can_view_reports").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CanViewPlan).HasColumnName("can_view_plan").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CanViewFinance).HasColumnName("can_view_finance").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CanViewCamera).HasColumnName("can_view_camera").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.GrantedAt).HasColumnName("granted_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");

        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => new { x.GuardianId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("guardian_portal_access_guardian_id_student_id_key");
    }
}
