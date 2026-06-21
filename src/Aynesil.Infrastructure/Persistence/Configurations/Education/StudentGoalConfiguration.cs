using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.student_goal.
/// Full audit: created_at, created_by, updated_at, updated_by.
/// Soft delete: deleted_at.
/// Self-referencing hierarchy: parent_goal_id → id.
/// </summary>
public class StudentGoalConfiguration : IEntityTypeConfiguration<StudentGoal>
{
    public void Configure(EntityTypeBuilder<StudentGoal> builder)
    {
        builder.ToTable("student_goal", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.TemplateId).HasColumnName("template_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.DevelopmentAreaId).HasColumnName("development_area_id");
        builder.Property(x => x.Horizon).HasColumnName("horizon").HasDefaultValue("short_term").IsRequired();
        builder.Property(x => x.ParentGoalId).HasColumnName("parent_goal_id");
        builder.Property(x => x.Statement).HasColumnName("statement").IsRequired();
        builder.Property(x => x.MasteryCriteria).HasColumnName("mastery_criteria");
        builder.Property(x => x.Baseline).HasColumnName("baseline");
        builder.Property(x => x.TargetValue).HasColumnName("target_value").HasPrecision(10, 2);
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("active").IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.TargetDate).HasColumnName("target_date");
        builder.Property(x => x.AchievedDate).HasColumnName("achieved_date");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.StudentId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_student_goal_student");

        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.ProgressRecords)
            .WithOne()
            .HasForeignKey(p => p.StudentGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ChildGoals)
            .WithOne()
            .HasForeignKey(g => g.ParentGoalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
