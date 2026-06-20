using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.case_note.
/// Audit: created_at, updated_at, deleted_at, row_version.
/// created_by and updated_by columns do NOT exist in the DB schema — both ignored.
/// authored_by is a separate column (who wrote it, may differ from the system user).
/// </summary>
public class CaseNoteConfiguration : IEntityTypeConfiguration<CaseNote>
{
    public void Configure(EntityTypeBuilder<CaseNote> builder)
    {
        builder.ToTable("case_note", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.NoteType).HasColumnName("note_type");
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();
        builder.Property(x => x.IsConfidential).HasColumnName("is_confidential").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.AuthoredBy).HasColumnName("authored_by");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // created_by and updated_by do NOT exist in DB
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.StudentId, x.IsConfidential, x.CreatedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_case_note_student");
    }
}
