using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class SurveyAnswerOptionConfiguration : IEntityTypeConfiguration<SurveyAnswerOption>
{
    public void Configure(EntityTypeBuilder<SurveyAnswerOption> builder)
    {
        builder.ToTable("survey_answer_option", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.QuestionId).HasColumnName("question_id").IsRequired();
        builder.Property(x => x.OptionText).HasColumnName("option_text").IsRequired();
        builder.Property(x => x.OptionValue).HasColumnName("option_value");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();

        builder.HasIndex(x => new { x.QuestionId, x.SortOrder })
            .HasDatabaseName("ix_survey_answer_option_question");

        builder.HasOne(x => x.Question)
            .WithMany(q => q.AnswerOptions)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
