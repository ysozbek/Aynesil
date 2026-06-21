using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.education_plan_goal.
/// Links a StudentGoal to an EducationPlan. Unique: (education_plan_id, student_goal_id).
/// No audit columns in DDL.
/// </summary>
public class EducationPlanGoalConfiguration : IEntityTypeConfiguration<EducationPlanGoal>
{
    public void Configure(EntityTypeBuilder<EducationPlanGoal> builder)
    {
        builder.ToTable("education_plan_goal", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducationPlanId).HasColumnName("education_plan_id").IsRequired();
        builder.Property(x => x.StudentGoalId).HasColumnName("student_goal_id").IsRequired();
        builder.Property(x => x.Horizon).HasColumnName("horizon").HasDefaultValue("short_term").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();

        builder.HasIndex(x => new { x.EducationPlanId, x.StudentGoalId })
            .IsUnique()
            .HasDatabaseName("education_plan_goal_education_plan_id_student_goal_id_key");

        builder.HasOne(x => x.Goal)
            .WithMany()
            .HasForeignKey(x => x.StudentGoalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
