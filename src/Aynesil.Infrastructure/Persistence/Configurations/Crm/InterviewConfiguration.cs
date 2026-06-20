using Aynesil.Domain.Modules.Crm.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Crm;

/// <summary>
/// Maps crm.interview.
/// Has created_at / updated_at / row_version but NOT deleted_at, created_by, or updated_by —
/// hence the entity inherits BaseEntity directly with its own subset of audit fields.
/// Status is enforced by DB check constraint ('scheduled'|'completed'|'no_show'|'cancelled').
/// </summary>
public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ToTable("interview", schema: "crm");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.LeadId).HasColumnName("lead_id").IsRequired();
        builder.Property(x => x.CampusId).HasColumnName("campus_id");
        builder.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(x => x.ConductedAt).HasColumnName("conducted_at");
        builder.Property(x => x.ConductedBy).HasColumnName("conducted_by");
        builder.Property(x => x.Outcome).HasColumnName("outcome");
        builder.Property(x => x.Recommendation).HasColumnName("recommendation");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasDefaultValue(InterviewStatus.Scheduled);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1).IsConcurrencyToken();

        builder.HasIndex(x => new { x.LeadId, x.ScheduledAt })
            .HasDatabaseName("ix_interview_lead");

        builder.HasIndex(x => new { x.CorporationId, x.Status })
            .HasDatabaseName("ix_interview_corp_status");
    }
}
