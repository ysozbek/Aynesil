using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.education_plan (BEP/IEP).
/// Full audit: created_at, created_by, updated_at, updated_by.
/// Soft delete: deleted_at.
/// guardian_visible: guardians may read this plan once approved and flag is set.
/// prepared_by / approved_by are FK to educators.educator.
/// </summary>
public class EducationPlanConfiguration : IEntityTypeConfiguration<EducationPlan>
{
    public void Configure(EntityTypeBuilder<EducationPlan> builder)
    {
        builder.ToTable("education_plan", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.AcademicPeriodId).HasColumnName("academic_period_id");
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue("draft").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to");
        builder.Property(x => x.PreparedBy).HasColumnName("prepared_by");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.GuardianVisible).HasColumnName("guardian_visible").HasDefaultValue(false).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.Ignore(x => x.IsDeleted);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasMany(x => x.PlanGoals)
            .WithOne(pg => pg.Plan)
            .HasForeignKey(pg => pg.EducationPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne()
            .HasForeignKey(r => r.EducationPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Approvals)
            .WithOne()
            .HasForeignKey(a => a.EducationPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Revisions)
            .WithOne()
            .HasForeignKey(r => r.EducationPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
