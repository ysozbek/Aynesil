using Aynesil.Domain.Modules.Crm.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Crm;

/// <summary>
/// Maps crm.lead_status_history.
/// Append-only — no soft delete, no row_version, no updated_at.
/// ON DELETE CASCADE is handled at the DB level and mirrored via EF.
/// </summary>
public class LeadStatusHistoryConfiguration : IEntityTypeConfiguration<LeadStatusHistory>
{
    public void Configure(EntityTypeBuilder<LeadStatusHistory> builder)
    {
        builder.ToTable("lead_status_history", schema: "crm");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.LeadId).HasColumnName("lead_id").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("status_id");
        builder.Property(x => x.PipelineStageId).HasColumnName("pipeline_stage_id");
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.ChangedBy).HasColumnName("changed_by");

        builder.HasIndex(x => new { x.LeadId, x.ChangedAt })
            .HasDatabaseName("ix_lead_status_history_lead");
    }
}
