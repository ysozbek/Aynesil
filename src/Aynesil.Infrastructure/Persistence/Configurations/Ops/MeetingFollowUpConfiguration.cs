using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

/// <summary>
/// EF Core configuration for ops.meeting_follow_up.
/// DDL: minimal audit — only created_at; no created_by, updated_at, deleted_at, row_version.
/// Status: 'open' | 'in_progress' | 'done' | 'cancelled' (hardcoded DDL CHECK).
/// No soft-delete query filter (child of meeting; cascade-deletes with the parent).
/// </summary>
public class MeetingFollowUpConfiguration : IEntityTypeConfiguration<MeetingFollowUp>
{
    public void Configure(EntityTypeBuilder<MeetingFollowUp> builder)
    {
        builder.ToTable("meeting_follow_up", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.MeetingId).HasColumnName("meeting_id").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").IsRequired();
        builder.Property(x => x.AssigneeId).HasColumnName("assignee_id");
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20)
            .HasDefaultValue("open").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Meeting)
            .WithMany(m => m.FollowUps)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
