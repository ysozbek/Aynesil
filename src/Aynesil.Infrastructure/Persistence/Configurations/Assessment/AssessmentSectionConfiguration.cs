using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_section.
/// No audit columns — sections are owned by the parent template and cascade-deleted with it.
/// </summary>
public class AssessmentSectionConfiguration : IEntityTypeConfiguration<AssessmentSection>
{
    public void Configure(EntityTypeBuilder<AssessmentSection> builder)
    {
        builder.ToTable("assessment_section", schema: "assessment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.DevelopmentAreaId).HasColumnName("development_area_id");

        // ── Navigations ───────────────────────────────────────────────────────
        builder.HasOne(x => x.Template)
            .WithMany(t => t.Sections)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.Section)
            .HasForeignKey(i => i.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Unique constraint ─────────────────────────────────────────────────
        builder.HasIndex(x => new { x.TemplateId, x.Code })
            .IsUnique()
            .HasDatabaseName("assessment_section_template_id_code_key");
    }
}
