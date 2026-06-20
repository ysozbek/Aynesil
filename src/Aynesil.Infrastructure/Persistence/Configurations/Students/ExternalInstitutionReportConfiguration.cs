using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.external_institution_report.
/// Audit: created_at, created_by, deleted_at, row_version ONLY.
/// updated_at and updated_by columns do NOT exist in the DB schema — both ignored here.
/// </summary>
public class ExternalInstitutionReportConfiguration
    : IEntityTypeConfiguration<ExternalInstitutionReport>
{
    public void Configure(EntityTypeBuilder<ExternalInstitutionReport> builder)
    {
        builder.ToTable("external_institution_report", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.InstitutionName).HasColumnName("institution_name").IsRequired();
        builder.Property(x => x.InstitutionTypeId).HasColumnName("institution_type_id");
        builder.Property(x => x.ReportDate).HasColumnName("report_date");
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.FileId).HasColumnName("file_id");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // updated_at and updated_by do NOT exist in DB
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.StudentId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_external_institution_report_student");
    }
}
