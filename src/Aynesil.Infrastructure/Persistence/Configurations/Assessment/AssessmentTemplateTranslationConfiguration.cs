using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Assessment;

/// <summary>
/// Maps assessment.assessment_template_translation.
/// Composite primary key (template_id, locale) — there is no surrogate id column.
/// Cascade-deleted when the parent template row is deleted.
/// </summary>
public class AssessmentTemplateTranslationConfiguration
    : IEntityTypeConfiguration<AssessmentTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<AssessmentTemplateTranslation> builder)
    {
        builder.ToTable("assessment_template_translation", schema: "assessment");

        builder.HasKey(x => new { x.TemplateId, x.Locale });

        builder.Property(x => x.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");

        builder.HasOne(x => x.Template)
            .WithMany(t => t.Translations)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
