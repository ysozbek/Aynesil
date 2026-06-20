using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Students;

/// <summary>
/// Maps students.development_report.
/// Audit: created_at, created_by, updated_at, deleted_at, row_version.
/// updated_by column does NOT exist in the DB schema — ignored here.
/// </summary>
public class DevelopmentReportConfiguration : IEntityTypeConfiguration<DevelopmentReport>
{
    public void Configure(EntityTypeBuilder<DevelopmentReport> builder)
    {
        builder.ToTable("development_report", schema: "students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.PeriodLabel).HasColumnName("period_label");
        builder.Property(x => x.ReportDate).HasColumnName("report_date");
        builder.Property(x => x.AuthoredBy).HasColumnName("authored_by");
        builder.Property(x => x.Content).HasColumnName("content");
        builder.Property(x => x.FileId).HasColumnName("file_id");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => new { x.StudentId, x.ReportDate })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_development_report_student");
    }
}
