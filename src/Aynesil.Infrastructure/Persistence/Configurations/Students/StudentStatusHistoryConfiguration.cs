using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.student_status_history.
/// Append-only — no soft delete, no update fields.
/// Inherits only BaseEntity (Id); all other columns are configured explicitly.
/// </summary>
public class StudentStatusHistoryConfiguration : IEntityTypeConfiguration<StudentStatusHistory>
{
    public void Configure(EntityTypeBuilder<StudentStatusHistory> builder)
    {
        builder.ToTable("student_status_history", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("status_id").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ChangedBy).HasColumnName("changed_by");

        builder.HasIndex(x => new { x.StudentId, x.ChangedAt })
            .HasDatabaseName("ix_student_status_history_student");
    }
}
