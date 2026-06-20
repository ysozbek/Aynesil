using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_report.
/// No soft-delete (no deleted_at column). No created_by / updated_by columns.
/// IsFinalized is a computed C# property derived from FinalizedAt — not mapped to a DB column.
/// finalized_by references iam.user_account — no EF navigation (cross-module, scalar FK only).
/// </summary>
public class AssessmentReportConfiguration : IEntityTypeConfiguration<AssessmentReport>
{
    public void Configure(EntityTypeBuilder<AssessmentReport> builder)
    {
        builder.ToTable("assessment_report", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.AssessmentSessionId).HasColumnName("assessment_session_id").IsRequired();

        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.Findings).HasColumnName("findings");
        builder.Property(x => x.FileId).HasColumnName("file_id");
        builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
        builder.Property(x => x.FinalizedBy).HasColumnName("finalized_by");

        // ── Audit (columns that DO exist in the DB) ───────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // ── Columns absent from DB — must be ignored ──────────────────────────
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        // IsFinalized is a computed C# property (not a DB column)
        builder.Ignore(x => x.IsFinalized);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(x => x.AssessmentSessionId)
            .HasDatabaseName("ix_assessment_report_session");

        builder.HasIndex(x => x.CorporationId)
            .HasDatabaseName("ix_assessment_report_corp");
    }
}
