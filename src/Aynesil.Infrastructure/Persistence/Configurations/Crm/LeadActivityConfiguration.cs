using Aynesil.Domain.Modules.Crm.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Crm;

/// <summary>
/// Maps crm.lead_activity.
/// Append-only communication log — no soft delete, no row_version, no updated_at.
/// ix_lead_activity_followup (partial) enables efficient follow-up dashboard queries.
/// </summary>
public class LeadActivityConfiguration : IEntityTypeConfiguration<LeadActivity>
{
    public void Configure(EntityTypeBuilder<LeadActivity> builder)
    {
        builder.ToTable("lead_activity", schema: "crm");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.LeadId).HasColumnName("lead_id").IsRequired();
        builder.Property(x => x.ActivityTypeId).HasColumnName("activity_type_id");
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(500);
        builder.Property(x => x.Body).HasColumnName("body");
        builder.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(10);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.FollowUpAt).HasColumnName("follow_up_at");
        builder.Property(x => x.PerformedBy).HasColumnName("performed_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        // Mirrors the DB partial index for follow-up dashboard queries
        builder.HasIndex(x => new { x.CorporationId, x.FollowUpAt })
            .HasFilter("follow_up_at IS NOT NULL")
            .HasDatabaseName("ix_lead_activity_followup");

        builder.HasIndex(x => x.LeadId)
            .HasDatabaseName("ix_lead_activity_lead");
    }
}
