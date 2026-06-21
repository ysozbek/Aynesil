using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.education_plan_approval.
/// Immutable approval ledger — decided_at only, no updated_at or row_version.
/// approver_id is an FK to educators.educator.
/// </summary>
public class EducationPlanApprovalConfiguration : IEntityTypeConfiguration<EducationPlanApproval>
{
    public void Configure(EntityTypeBuilder<EducationPlanApproval> builder)
    {
        builder.ToTable("education_plan_approval", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducationPlanId).HasColumnName("education_plan_id").IsRequired();
        builder.Property(x => x.ApproverId).HasColumnName("approver_id");
        builder.Property(x => x.Decision).HasColumnName("decision").IsRequired();
        builder.Property(x => x.Comment).HasColumnName("comment");
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at").HasDefaultValueSql("now()").IsRequired();
    }
}
