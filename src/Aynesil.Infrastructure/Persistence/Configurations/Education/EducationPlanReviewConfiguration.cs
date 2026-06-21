using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.education_plan_review.
/// Immutable review ledger — only created_at, no updated_at or row_version.
/// reviewer_id is an FK to educators.educator (no navigation needed from this side).
/// </summary>
public class EducationPlanReviewConfiguration : IEntityTypeConfiguration<EducationPlanReview>
{
    public void Configure(EntityTypeBuilder<EducationPlanReview> builder)
    {
        builder.ToTable("education_plan_review", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducationPlanId).HasColumnName("education_plan_id").IsRequired();
        builder.Property(x => x.ReviewedOn).HasColumnName("reviewed_on").HasDefaultValueSql("current_date").IsRequired();
        builder.Property(x => x.ReviewerId).HasColumnName("reviewer_id");
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.Outcome).HasColumnName("outcome");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
    }
}
