using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.program_recommendation.
/// No soft-delete. No created_by / updated_by columns.
/// assessment_session_id, lead_id, student_id are all optional.
/// recommended_program_id is a soft FK to education.program — no EF navigation.
/// recommended_by is a soft FK to educators.educator — no EF navigation.
/// </summary>
public class ProgramRecommendationConfiguration : IEntityTypeConfiguration<ProgramRecommendation>
{
    public void Configure(EntityTypeBuilder<ProgramRecommendation> builder)
    {
        builder.ToTable("program_recommendation", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.AssessmentSessionId).HasColumnName("assessment_session_id");
        builder.Property(x => x.LeadId).HasColumnName("lead_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");

        // Soft FK — education.program; no EF navigation to avoid cross-module coupling.
        builder.Property(x => x.RecommendedProgramId).HasColumnName("recommended_program_id");

        builder.Property(x => x.RecommendedIntensity).HasColumnName("recommended_intensity");
        builder.Property(x => x.Rationale).HasColumnName("rationale");

        // Soft FK — educators.educator; no EF navigation.
        builder.Property(x => x.RecommendedBy).HasColumnName("recommended_by");

        // ── Audit (columns that DO exist in the DB) ───────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        // ── Columns absent from DB — must be ignored ──────────────────────────
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(x => x.AssessmentSessionId)
            .HasFilter("assessment_session_id IS NOT NULL")
            .HasDatabaseName("ix_program_recommendation_session");

        builder.HasIndex(x => x.LeadId)
            .HasFilter("lead_id IS NOT NULL")
            .HasDatabaseName("ix_program_recommendation_lead");

        builder.HasIndex(x => x.StudentId)
            .HasFilter("student_id IS NOT NULL")
            .HasDatabaseName("ix_program_recommendation_student");
    }
}
