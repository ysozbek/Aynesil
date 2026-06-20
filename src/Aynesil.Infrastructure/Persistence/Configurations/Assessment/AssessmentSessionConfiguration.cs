using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_session.
/// Soft-delete via deleted_at — the global query filter excludes soft-deleted rows.
/// updated_by column does NOT exist in the DB; the AuditableEntity.UpdatedBy property is ignored.
/// student_id and assessor_id reference tables from other modules (students, educators) —
/// no EF navigation properties are configured to avoid cross-module coupling.
/// </summary>
public class AssessmentSessionConfiguration : IEntityTypeConfiguration<AssessmentSession>
{
    public void Configure(EntityTypeBuilder<AssessmentSession> builder)
    {
        builder.ToTable("assessment_session", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        // ── Tenant scope ──────────────────────────────────────────────────────
        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();

        // ── Template snapshot ─────────────────────────────────────────────────
        builder.Property(x => x.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(x => x.TemplateVersion).HasColumnName("template_version").HasDefaultValue(1).IsRequired();

        // ── Subject FKs (lead OR student — DB enforces chk_subject) ──────────
        builder.Property(x => x.LeadId).HasColumnName("lead_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");

        // ── Other FKs (cross-module: no navigation properties) ────────────────
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.AssessorId).HasColumnName("assessor_id");

        // ── Scheduling ────────────────────────────────────────────────────────
        builder.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(x => x.PerformedAt).HasColumnName("performed_at");

        // ── Workflow ──────────────────────────────────────────────────────────
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("planned")
            .IsRequired();

        builder.Property(x => x.TotalScore).HasColumnName("total_score").HasPrecision(10, 2);

        // ── Audit (columns that DO exist in the DB) ───────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // ── Columns absent from DB — must be ignored ──────────────────────────
        builder.Ignore(x => x.UpdatedBy);

        // ── Soft-delete global query filter ───────────────────────────────────
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // ── One-to-many navigations ───────────────────────────────────────────
        builder.HasMany(x => x.Responses)
            .WithOne(r => r.Session)
            .HasForeignKey(r => r.AssessmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Recommendations)
            .WithOne()
            .HasForeignKey(r => r.AssessmentSessionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CorporationId, x.Status })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_assessment_session_corp_status");

        builder.HasIndex(x => x.LeadId)
            .HasFilter("deleted_at IS NULL AND lead_id IS NOT NULL")
            .HasDatabaseName("ix_assessment_session_lead");

        builder.HasIndex(x => x.StudentId)
            .HasFilter("deleted_at IS NULL AND student_id IS NOT NULL")
            .HasDatabaseName("ix_assessment_session_student");

        builder.HasIndex(x => new { x.CorporationId, x.ScheduledAt })
            .HasFilter("deleted_at IS NULL AND scheduled_at IS NOT NULL")
            .HasDatabaseName("ix_assessment_session_scheduled");
    }
}
