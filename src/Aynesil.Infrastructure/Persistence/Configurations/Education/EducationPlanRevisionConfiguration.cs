using Aynesil.Domain.Modules.Education.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Education;

/// <summary>
/// Maps education.education_plan_revision.
/// Immutable revision ledger with a jsonb snapshot column.
/// revised_at only, no updated_at or row_version.
/// Snapshot stored as JsonDocument — Npgsql maps it natively to PostgreSQL jsonb.
/// </summary>
public class EducationPlanRevisionConfiguration : IEntityTypeConfiguration<EducationPlanRevision>
{
    public void Configure(EntityTypeBuilder<EducationPlanRevision> builder)
    {
        builder.ToTable("education_plan_revision", schema: "education");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.EducationPlanId).HasColumnName("education_plan_id").IsRequired();
        builder.Property(x => x.FromVersion).HasColumnName("from_version").IsRequired();
        builder.Property(x => x.ToVersion).HasColumnName("to_version").IsRequired();
        builder.Property(x => x.ChangeSummary).HasColumnName("change_summary");
        builder.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        builder.Property(x => x.RevisedBy).HasColumnName("revised_by");
        builder.Property(x => x.RevisedAt).HasColumnName("revised_at").HasDefaultValueSql("now()").IsRequired();
    }
}
