using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

/// <summary>
/// EF Core configuration for ops.meeting_outcome.
/// DDL: minimal audit — only created_at and created_by; no updated_at, deleted_at, row_version.
/// No soft-delete query filter (child of meeting; cascade-deletes with the parent).
/// </summary>
public class MeetingOutcomeConfiguration : IEntityTypeConfiguration<MeetingOutcome>
{
    public void Configure(EntityTypeBuilder<MeetingOutcome> builder)
    {
        builder.ToTable("meeting_outcome", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.MeetingId).HasColumnName("meeting_id").IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary");
        builder.Property(x => x.Decisions).HasColumnName("decisions");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");

        builder.HasOne(x => x.Meeting)
            .WithMany(m => m.Outcomes)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
