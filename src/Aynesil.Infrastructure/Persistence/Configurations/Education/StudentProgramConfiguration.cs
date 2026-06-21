using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.student_program.
/// Audit: created_at, updated_at only (created_by/updated_by absent from DDL).
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// Status: checked text column — active | paused | completed | cancelled.
/// </summary>
public class StudentProgramConfiguration : IEntityTypeConfiguration<StudentProgram>
{
    public void Configure(EntityTypeBuilder<StudentProgram> builder)
    {
        builder.ToTable("student_program", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.EndDate).HasColumnName("end_date");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20)
            .HasDefaultValue("active").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.StudentId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_student_program_student");

        builder.HasOne(x => x.Program)
            .WithMany(p => p.StudentPrograms)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
