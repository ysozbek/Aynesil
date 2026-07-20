using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
    {
        builder.ToTable("survey_question", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.SurveyId).HasColumnName("survey_id").IsRequired();
        builder.Property(x => x.QuestionText).HasColumnName("question_text").IsRequired();
        builder.Property(x => x.QuestionType).HasColumnName("question_type").HasMaxLength(30)
            .HasDefaultValue("text").IsRequired();
        builder.Property(x => x.IsRequired).HasColumnName("is_required").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");

        builder.HasIndex(x => new { x.SurveyId, x.SortOrder })
            .HasDatabaseName("ix_survey_question_survey");

        builder.HasOne(x => x.Survey)
            .WithMany(s => s.Questions)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AnswerOptions)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
