using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.emergency_contact.
/// No audit columns, no soft delete. Replace-as-set semantics in the application layer.
/// </summary>
public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("emergency_contact", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
        builder.Property(x => x.Relationship).HasColumnName("relationship");
        builder.Property(x => x.Phone).HasColumnName("phone").IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(1).IsRequired();

        builder.HasIndex(x => new { x.StudentId, x.Priority })
            .HasDatabaseName("ix_emergency_contact_student_priority");
    }
}
