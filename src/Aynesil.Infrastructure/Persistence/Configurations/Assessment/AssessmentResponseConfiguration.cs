using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_response.
/// Append-only: no audit columns, no soft delete.
/// Unique constraint ensures one response per item per session.
/// </summary>
public class AssessmentResponseConfiguration : IEntityTypeConfiguration<AssessmentResponse>
{
    public void Configure(EntityTypeBuilder<AssessmentResponse> builder)
    {
        builder.ToTable("assessment_response", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.AssessmentSessionId).HasColumnName("assessment_session_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();

        builder.Property(x => x.NumericValue).HasColumnName("numeric_value").HasPrecision(10, 2);
        builder.Property(x => x.TextValue).HasColumnName("text_value");
        builder.Property(x => x.ChoiceValue).HasColumnName("choice_value");
        builder.Property(x => x.Note).HasColumnName("note");

        // ── Navigations ───────────────────────────────────────────────────────
        builder.HasOne(x => x.Session)
            .WithMany(s => s.Responses)
            .HasForeignKey(x => x.AssessmentSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Unique: one response per item per session ─────────────────────────
        builder.HasIndex(x => new { x.AssessmentSessionId, x.ItemId })
            .IsUnique()
            .HasDatabaseName("assessment_response_assessment_session_id_item_id_key");
    }
}
