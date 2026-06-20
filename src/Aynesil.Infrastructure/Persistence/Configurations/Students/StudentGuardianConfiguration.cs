using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.student_guardian.
/// No audit columns, no soft delete.
/// Unique: (student_id, guardian_id).
/// </summary>
public class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> builder)
    {
        builder.ToTable("student_guardian", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id").IsRequired();
        builder.Property(x => x.RelationshipId).HasColumnName("relationship_id");
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.HasCustody).HasColumnName("has_custody").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.PortalAccess).HasColumnName("portal_access").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.FinancialResponsible).HasColumnName("financial_responsible").HasDefaultValue(false).IsRequired();

        builder.HasIndex(x => new { x.StudentId, x.GuardianId })
            .IsUnique()
            .HasDatabaseName("student_guardian_student_id_guardian_id_key");
    }
}
