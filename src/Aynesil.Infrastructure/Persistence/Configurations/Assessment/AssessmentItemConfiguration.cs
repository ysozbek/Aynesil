using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_item.
/// No audit columns — items are owned by the parent section and cascade-deleted with it.
/// Choices column is JSONB — stored as raw JSON string, serialised by the application layer.
/// </summary>
public class AssessmentItemConfiguration : IEntityTypeConfiguration<AssessmentItem>
{
    public void Configure(EntityTypeBuilder<AssessmentItem> builder)
    {
        builder.ToTable("assessment_item", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Prompt).HasColumnName("prompt").IsRequired();
        builder.Property(x => x.ResponseType).HasColumnName("response_type").HasMaxLength(20).IsRequired();

        // JSONB stored as raw string; the application layer handles serialisation.
        builder.Property(x => x.Choices)
            .HasColumnName("choices")
            .HasColumnType("jsonb");

        builder.Property(x => x.Weight)
            .HasColumnName("weight")
            .HasPrecision(6, 2)
            .HasDefaultValue(1m)
            .IsRequired();

        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();

        // ── Navigation ────────────────────────────────────────────────────────
        builder.HasOne(x => x.Section)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Unique constraint ─────────────────────────────────────────────────
        builder.HasIndex(x => new { x.SectionId, x.Code })
            .IsUnique()
            .HasDatabaseName("assessment_item_section_id_code_key");
    }
}
