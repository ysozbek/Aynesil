using Aynesil.Domain.Modules.Camps.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aynesil.Infrastructure.Persistence.Configurations.Camps;

public class CampActivityParticipationConfiguration
    : IEntityTypeConfiguration<CampActivityParticipation>
{
    public void Configure(EntityTypeBuilder<CampActivityParticipation> builder)
    {
        builder.ToTable("camp_activity_participation", schema: "camps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("core.uuid_generate_v7()");

        builder.Property(x => x.CorporationId).HasColumnName("corporation_id").IsRequired();
        builder.Property(x => x.CampActivityId).HasColumnName("camp_activity_id").IsRequired();
        builder.Property(x => x.CampEnrollmentId).HasColumnName("camp_enrollment_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("registered")
            .IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.RecordedBy).HasColumnName("recorded_by");
        builder.Property(x => x.RecordedAt)
            .HasColumnName("recorded_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.CampActivityId, x.CampEnrollmentId }).IsUnique();

        builder.HasOne(x => x.Enrollment)
            .WithMany()
            .HasForeignKey(x => x.CampEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
