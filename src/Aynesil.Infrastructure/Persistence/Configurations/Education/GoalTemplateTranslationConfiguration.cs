using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.goal_template_translation.
/// Composite primary key (goal_template_id, locale) — no surrogate id column in DDL.
/// Cascade-deleted when parent template is deleted.
/// </summary>
public class GoalTemplateTranslationConfiguration : IEntityTypeConfiguration<GoalTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<GoalTemplateTranslation> builder)
    {
        builder.ToTable("goal_template_translation", schema: "education");

        builder.HasKey(x => new { x.GoalTemplateId, x.Locale });

        builder.Property(x => x.GoalTemplateId).HasColumnName("goal_template_id").IsRequired();
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Statement).HasColumnName("statement").IsRequired();
        builder.Property(x => x.DefaultCriteria).HasColumnName("default_criteria");

        builder.HasOne(x => x.Template)
            .WithMany(t => t.Translations)
            .HasForeignKey(x => x.GoalTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
