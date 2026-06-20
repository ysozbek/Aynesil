using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.student_campus.
/// No audit columns, no soft delete. Unique: (student_id, campus_id).
/// Enrollments are closed by setting active_to, never physically deleted.
/// </summary>
public class StudentCampusConfiguration : IEntityTypeConfiguration<StudentCampus>
{
    public void Configure(EntityTypeBuilder<StudentCampus> builder)
    {
        builder.ToTable("student_campus", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id").IsRequired();
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ActiveFrom).HasColumnName("active_from").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.ActiveTo).HasColumnName("active_to");

        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => new { x.StudentId, x.CampusId })
            .IsUnique()
            .HasDatabaseName("student_campus_student_id_campus_id_key");
    }
}
