using Aynesil.Domain.Modules.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Finance;

/// <summary>
/// Maps finance.scholarship.
/// No soft delete (no deleted_at in DDL) — scholarships are historical records.
/// Audit: created_at, updated_at, row_version (no created_by / updated_by in DDL).
/// scholarship_type_id references ref.ref_value (ref_type 'scholarship_type') — configurable.
/// Either percentage or amount is set; not both.
/// </summary>
public class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> builder)
    {
        builder.ToTable("scholarship", schema: "finance");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.ScholarshipTypeId).HasColumnName("scholarship_type_id");
        builder.Property(x => x.Percentage).HasColumnName("percentage")
            .HasColumnType("numeric(5,2)");
        builder.Property(x => x.Amount).HasColumnName("amount")
            .HasColumnType("numeric(14,2)");
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from")
            .HasColumnType("date");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to")
            .HasColumnType("date");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.Note).HasColumnName("note");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version")
            .HasDefaultValue(1).IsConcurrencyToken();

        // Not in DDL
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.CorporationId, x.StudentId })
            .HasDatabaseName("ix_scholarship_student");
    }
}
