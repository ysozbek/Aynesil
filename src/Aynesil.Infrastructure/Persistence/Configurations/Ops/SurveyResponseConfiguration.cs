using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("survey_response", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SurveyId).HasColumnName("survey_id").IsRequired();
        builder.Property(x => x.RespondentUserId).HasColumnName("respondent_user_id");
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        // ops.survey_response DDL has only created_at — ignore full SoftDeleteEntity audit columns
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.IsDeleted);

        builder.HasIndex(x => new { x.SurveyId, x.SubmittedAt })
            .HasDatabaseName("ix_survey_response_survey");
        builder.HasIndex(x => new { x.GuardianId, x.SurveyId })
            .HasFilter("guardian_id IS NOT NULL")
            .HasDatabaseName("ix_survey_response_guardian");
        builder.HasIndex(x => new { x.StudentId, x.SurveyId })
            .HasFilter("student_id IS NOT NULL")
            .HasDatabaseName("ix_survey_response_student");

        builder.HasOne(x => x.Survey)
            .WithMany()
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.QuestionResponses)
            .WithOne(qr => qr.Response)
            .HasForeignKey(qr => qr.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
