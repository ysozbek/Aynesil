using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

/// <summary>
/// EF Core configuration for ops.educator_performance_snapshot.
///
/// The table has no standard audit columns (created_at/updated_at/created_by/updated_by/
/// deleted_at/row_version). All inherited properties that don't map to real columns are
/// explicitly Ignored, following the same pattern as KpiValueConfiguration and
/// ParentFeedbackConfiguration.
///
/// ComputedAt replaces the standard audit timestamp for this snapshot entity.
/// No global query filter is applied (no soft-delete on this table).
/// </summary>
public class EducatorPerformanceSnapshotConfiguration
    : IEntityTypeConfiguration<EducatorPerformanceSnapshot>
{
    public void Configure(EntityTypeBuilder<EducatorPerformanceSnapshot> builder)
    {
        builder.ToTable("educator_performance_snapshot", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId)
            .HasColumnName("corporation_id")
            .IsRequired();

        builder.Property(x => x.EducatorId)
            .HasColumnName("educator_id")
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .HasColumnName("period_start")
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .HasColumnName("period_end")
            .IsRequired();

        builder.Property(x => x.SessionCount)
            .HasColumnName("session_count");

        builder.Property(x => x.AttendanceRate)
            .HasColumnName("attendance_rate")
            .HasColumnType("numeric(5,2)");

        builder.Property(x => x.GoalAchievementRate)
            .HasColumnName("goal_achievement_rate")
            .HasColumnType("numeric(5,2)");

        builder.Property(x => x.ParentFeedbackAvg)
            .HasColumnName("parent_feedback_avg")
            .HasColumnType("numeric(4,2)");

        builder.Property(x => x.UtilizationRate)
            .HasColumnName("utilization_rate")
            .HasColumnType("numeric(5,2)");

        builder.Property(x => x.Detail)
            .HasColumnName("detail")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(x => x.ComputedAt)
            .HasColumnName("computed_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // ops.educator_performance_snapshot DDL has no standard audit columns.
        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.IsDeleted);

        // Unique constraint: one snapshot per (educator, period).
        builder.HasIndex(x => new { x.EducatorId, x.PeriodStart, x.PeriodEnd })
            .IsUnique()
            .HasDatabaseName("uq_educator_perf_snapshot_period");
    }
}
