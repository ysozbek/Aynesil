using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class SurveyQuestionResponseConfiguration : IEntityTypeConfiguration<SurveyQuestionResponse>
{
    public void Configure(EntityTypeBuilder<SurveyQuestionResponse> builder)
    {
        builder.ToTable("survey_question_response", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.ResponseId).HasColumnName("response_id").IsRequired();
        builder.Property(x => x.QuestionId).HasColumnName("question_id").IsRequired();
        builder.Property(x => x.AnswerText).HasColumnName("answer_text");
        builder.Property(x => x.AnswerOptionId).HasColumnName("answer_option_id");
        builder.Property(x => x.NumericValue).HasColumnName("numeric_value").HasColumnType("numeric(6,2)");

        builder.HasIndex(x => new { x.ResponseId, x.QuestionId })
            .IsUnique()
            .HasDatabaseName("survey_question_response_response_id_question_id_key");

        builder.HasOne(x => x.Response)
            .WithMany(r => r.QuestionResponses)
            .HasForeignKey(x => x.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AnswerOption)
            .WithMany()
            .HasForeignKey(x => x.AnswerOptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
