using Aynesil.Domain.Modules.Ops.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Ops;

public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.ToTable("meeting_participant", schema: "ops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.MeetingId).HasColumnName("meeting_id").IsRequired();
        builder.Property(x => x.ParticipantType).HasColumnName("participant_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.GuardianId).HasColumnName("guardian_id");
        builder.Property(x => x.LeadId).HasColumnName("lead_id");
        builder.Property(x => x.ExternalName).HasColumnName("external_name");
        builder.Property(x => x.Attendance).HasColumnName("attendance").HasMaxLength(20);

        // ops.meeting_participant DDL has no audit columns beyond corporation_id
        builder.Ignore(x => x.CreatedAt);
        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedAt);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DeletedAt);
        builder.Ignore(x => x.RowVersion);
        builder.Ignore(x => x.IsDeleted);

        builder.HasOne(x => x.Meeting)
            .WithMany(m => m.Participants)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
