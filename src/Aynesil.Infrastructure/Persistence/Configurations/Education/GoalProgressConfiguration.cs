using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.goal_progress.
/// Immutable append-only records — only created_at, no updated_at or row_version.
/// session_id is a soft reference to scheduling.session (no FK constraint in DDL).
/// No soft-delete: these are measurement records and must never be deleted.
/// </summary>
public class GoalProgressConfiguration : IEntityTypeConfiguration<GoalProgress>
{
    public void Configure(EntityTypeBuilder<GoalProgress> builder)
    {
        builder.ToTable("goal_progress", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentGoalId).HasColumnName("student_goal_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.MeasuredOn).HasColumnName("measured_on").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.MeasuredValue).HasColumnName("measured_value").HasPrecision(10, 2);
        builder.Property(x => x.PercentComplete).HasColumnName("percent_complete").HasPrecision(5, 2);
        builder.Property(x => x.Trend).HasColumnName("trend");
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.RecordedBy).HasColumnName("recorded_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(x => new { x.StudentGoalId, x.MeasuredOn })
            .HasDatabaseName("ix_goal_progress_goal_time");
    }
}
