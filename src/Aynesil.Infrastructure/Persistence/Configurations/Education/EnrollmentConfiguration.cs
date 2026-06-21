using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.enrollment.
/// Full audit: created_at, created_by, updated_at, updated_by.
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// </summary>
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollment", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.EnrolledOn).HasColumnName("enrolled_on").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.EndedOn).HasColumnName("ended_on");
        builder.Property(x => x.TerminationReason).HasColumnName("termination_reason");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.CorporationId, x.StudentId })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_enrollment_corp_student");

        builder.HasMany(x => x.StudentPrograms)
            .WithOne()
            .HasForeignKey(sp => sp.EnrollmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
