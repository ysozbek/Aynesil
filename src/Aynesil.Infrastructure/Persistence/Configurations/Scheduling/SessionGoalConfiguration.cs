using Aynesil.Domain.Modules.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Scheduling;

/// <summary>
/// Maps scheduling.session_goal.
/// Links a student goal to a session for inline progress tracking.
/// Unique: (session_id, student_goal_id). No audit columns. Cascade-deleted from session.
/// </summary>
public class SessionGoalConfiguration : IEntityTypeConfiguration<SessionGoal>
{
    public void Configure(EntityTypeBuilder<SessionGoal> builder)
    {
        builder.ToTable("session_goal", schema: "scheduling");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.StudentGoalId).HasColumnName("student_goal_id").IsRequired();
        builder.Property(x => x.WorkedOn).HasColumnName("worked_on").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.ProgressNote).HasColumnName("progress_note");
        builder.Property(x => x.MeasuredValue).HasColumnName("measured_value").HasPrecision(10, 2);

        builder.HasIndex(x => new { x.SessionId, x.StudentGoalId })
            .IsUnique()
            .HasDatabaseName("session_goal_session_id_student_goal_id_key");
    }
}
