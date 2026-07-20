using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.student_care_assignment (ABAC Phase 2/4).
/// The table has all standard audit columns including updated_by.
/// Self-FK source_assignment_id supports delegation/substitution provenance (design §7).
/// RLS policies (Phase 3) are defined in the DB; EF global query filter handles soft-delete only.
/// </summary>
public class StudentCareAssignmentConfiguration : IEntityTypeConfiguration<StudentCareAssignment>
{
    public void Configure(EntityTypeBuilder<StudentCareAssignment> builder)
    {
        builder.ToTable("student_care_assignment", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.EducatorId).HasColumnName("educator_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        builder.Property(x => x.ActiveFrom).HasColumnName("active_from").IsRequired();
        builder.Property(x => x.ActiveTo).HasColumnName("active_to");

        // Provenance columns (design §7: delegation/substitution/emergency)
        builder.Property(x => x.GrantTypeId).HasColumnName("grant_type_id");
        builder.Property(x => x.SourceAssignmentId).HasColumnName("source_assignment_id");
        builder.Property(x => x.GrantedBy).HasColumnName("granted_by");
        builder.Property(x => x.Reason).HasColumnName("reason");

        // Audit columns — updated_by IS present in this table (unlike medical_report etc.)
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        // Global query filter: exclude soft-deleted rows at app layer
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // Self-referencing FK: source_assignment_id → parent assignment (delegation provenance)
        builder.HasOne<StudentCareAssignment>()
            .WithMany()
            .HasForeignKey(x => x.SourceAssignmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Key indexes for RLS policy lookup (mirrors DB indexes from Phase 2)
        builder.HasIndex(x => new { x.EducatorId, x.StudentId })
            .HasFilter("status = 'active' AND deleted_at IS NULL")
            .HasDatabaseName("ix_care_assignment_educator_student");

        builder.HasIndex(x => new { x.StudentId, x.Status })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_care_assignment_student");
    }
}
